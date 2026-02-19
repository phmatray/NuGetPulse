using NuGetPulse.Core.Models;
using NuGetPulse.Graph.Models;

namespace NuGetPulse.Graph;

/// <summary>
/// Builds a <see cref="DependencyGraph"/> from a flat list of scanned package references.
///
/// Each distinct package name becomes a node; each project file that references it
/// creates an edge from the project node to the package node.
/// Version conflicts (same package, different versions across projects) are detected
/// and annotated on the nodes, ported from NugetManager's DependencyGraphService logic.
/// </summary>
public sealed class DependencyGraphBuilder : IDependencyGraphBuilder
{
    public DependencyGraph Build(
        IReadOnlyList<PackageReference> packages,
        DependencyGraphOptions? options = null)
    {
        options ??= new DependencyGraphOptions();

        var graph = new DependencyGraph();
        var packageVersionMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        // Build one node per (PackageName, Version) pair — distinct across all project files
        var distinctPackages = packages
            .GroupBy(p => $"{p.PackageName}||{p.Version}", StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in distinctPackages)
        {
            var parts = group.Key.Split("||", 2);
            var name = parts[0];
            var version = parts.Length > 1 ? parts[1] : string.Empty;
            var nodeId = $"{name}_{version}";

            if (!packageVersionMap.ContainsKey(name))
                packageVersionMap[name] = [];
            packageVersionMap[name].Add(version);

            var node = new DependencyNode
            {
                Id = nodeId,
                PackageId = name,
                Version = version,
                Label = $"{name} {version}",
                Type = NodeType.RootPackage,
                ProjectFile = group.First().ProjectFile
            };

            graph.Nodes.Add(node);
        }

        // Build project-file nodes and edges
        var projectFiles = packages
            .Select(p => p.ProjectFile)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var projectFile in projectFiles)
        {
            var projectNodeId = $"project_{Path.GetFileName(projectFile)}_{projectFile.GetHashCode():X}";
            var projectNode = new DependencyNode
            {
                Id = projectNodeId,
                PackageId = Path.GetFileName(projectFile),
                Version = string.Empty,
                Label = Path.GetFileName(projectFile),
                Type = NodeType.RootPackage,
                ProjectFile = projectFile
            };
            graph.Nodes.Add(projectNode);

            // Edges: project → each package it references
            foreach (var pkg in packages.Where(p =>
                string.Equals(p.ProjectFile, projectFile, StringComparison.OrdinalIgnoreCase)))
            {
                var targetId = $"{pkg.PackageName}_{pkg.Version}";
                var edge = new DependencyEdge
                {
                    Id = $"{projectNodeId}_to_{targetId}",
                    Source = projectNodeId,
                    Target = targetId,
                    Type = EdgeType.Direct
                };
                graph.Edges.Add(edge);
            }
        }

        // Detect and mark conflicts
        if (options.HighlightConflicts)
            DetectConflicts(graph, packageVersionMap);

        return graph;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static void DetectConflicts(
        DependencyGraph graph,
        Dictionary<string, HashSet<string>> packageVersionMap)
    {
        foreach (var (packageName, versions) in packageVersionMap.Where(kv => kv.Value.Count > 1))
        {
            var conflictNodes = graph.Nodes
                .Where(n => string.Equals(n.PackageId, packageName, StringComparison.OrdinalIgnoreCase)
                         && n.Type == NodeType.RootPackage)
                .ToList();

            if (conflictNodes.Count <= 1) continue;

            var severity = CalculateSeverity(versions);
            var suggested = versions.OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase).First();

            var conflict = new ConflictInfo
            {
                PackageId = packageName,
                Versions = [.. versions.OrderBy(v => v)],
                NodeIds = conflictNodes.Select(n => n.Id).ToList(),
                Severity = severity,
                SuggestedVersion = suggested
            };

            graph.Conflicts[packageName] = conflict;

            foreach (var node in conflictNodes)
            {
                node.HasConflict = true;
                node.ConflictSeverity = severity;
            }

            // Conflict edges between all conflicting version nodes
            for (int i = 0; i < conflictNodes.Count - 1; i++)
            {
                for (int j = i + 1; j < conflictNodes.Count; j++)
                {
                    graph.Edges.Add(new DependencyEdge
                    {
                        Id = $"conflict_{conflictNodes[i].Id}_to_{conflictNodes[j].Id}",
                        Source = conflictNodes[i].Id,
                        Target = conflictNodes[j].Id,
                        Type = EdgeType.Conflict,
                        IsConflict = true
                    });
                }
            }
        }
    }

    private static int CalculateSeverity(HashSet<string> versions)
    {
        if (versions.Count <= 1) return 0;

        var parsed = versions
            .Select(v => v.Split('-')[0].Split('.').Take(3)
                .Select(part => int.TryParse(part, out var n) ? n : 0).ToArray())
            .ToList();

        var hasMajorDiff = false;
        var hasMinorDiff = false;

        for (int i = 0; i < parsed.Count - 1; i++)
        {
            for (int j = i + 1; j < parsed.Count; j++)
            {
                var a = parsed[i];
                var b = parsed[j];
                if (a.Length > 0 && b.Length > 0 && a[0] != b[0]) hasMajorDiff = true;
                else if (a.Length > 1 && b.Length > 1 && a[1] != b[1]) hasMinorDiff = true;
            }
        }

        return hasMajorDiff ? 3 : hasMinorDiff ? 2 : 1;
    }
}
