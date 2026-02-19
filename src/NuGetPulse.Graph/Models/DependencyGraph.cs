namespace NuGetPulse.Graph.Models;

/// <summary>
/// Represents a package dependency graph with nodes, edges, and detected conflicts.
/// Ported from NugetManager's IDependencyGraphService / DependencyGraph model.
/// </summary>
public sealed class DependencyGraph
{
    public List<DependencyNode> Nodes { get; init; } = [];
    public List<DependencyEdge> Edges { get; init; } = [];

    /// <summary>Package name → conflict info for packages with multiple versions in use.</summary>
    public Dictionary<string, ConflictInfo> Conflicts { get; init; } = [];

    /// <summary>Summary counts.</summary>
    public int NodeCount => Nodes.Count;
    public int EdgeCount => Edges.Count;
    public int ConflictCount => Conflicts.Count;
    public int RootPackageCount => Nodes.Count(n => n.Type == NodeType.RootPackage);
}

public sealed class DependencyNode
{
    public string Id { get; init; } = string.Empty;
    public string PackageId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public NodeType Type { get; init; }
    public string? ProjectFile { get; init; }
    public bool HasConflict { get; set; }
    public int ConflictSeverity { get; set; }
    public string? IconUrl { get; init; }
    public long TotalDownloads { get; init; }
    public bool IsVerified { get; init; }
}

public sealed class DependencyEdge
{
    public string Id { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public EdgeType Type { get; init; }
    public string? VersionRange { get; init; }
    public bool IsConflict { get; init; }
}

public sealed class ConflictInfo
{
    public string PackageId { get; init; } = string.Empty;
    public List<string> Versions { get; init; } = [];
    public List<string> NodeIds { get; init; } = [];

    /// <summary>1 = patch, 2 = minor, 3 = major.</summary>
    public int Severity { get; init; }
    public string? SuggestedVersion { get; init; }

    public string SeverityLabel => Severity switch
    {
        3 => "High",
        2 => "Medium",
        1 => "Low",
        _ => "Unknown"
    };
}

public enum NodeType
{
    RootPackage,
    DirectDependency,
    TransitiveDependency,
    ConflictNode
}

public enum EdgeType
{
    Direct,
    Transitive,
    Conflict
}

/// <summary>Options for building a dependency graph.</summary>
public sealed class DependencyGraphOptions
{
    public bool HighlightConflicts { get; init; } = true;
}
