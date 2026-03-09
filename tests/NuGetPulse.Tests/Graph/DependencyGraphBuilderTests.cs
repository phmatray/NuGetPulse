using Shouldly;
using NuGetPulse.Core.Models;
using NuGetPulse.Graph;
using NuGetPulse.Graph.Models;

namespace NuGetPulse.Tests.Graph;

/// <summary>Unit tests for <see cref="DependencyGraphBuilder"/>.</summary>
public sealed class DependencyGraphBuilderTests
{
    private readonly DependencyGraphBuilder _sut = new();

    [Fact]
    public void Build_EmptyPackageList_ReturnsEmptyGraph()
    {
        var graph = _sut.Build([]);

        graph.Nodes.ShouldBeEmpty();
        graph.Edges.ShouldBeEmpty();
        graph.Conflicts.ShouldBeEmpty();
    }

    [Fact]
    public void Build_SinglePackageSingleProject_CreatesPackageAndProjectNodes()
    {
        var packages = new List<PackageReference>
        {
            new() { PackageName = "Newtonsoft.Json", Version = "13.0.3", ProjectFile = "/src/App/App.csproj" }
        };

        var graph = _sut.Build(packages);

        graph.Nodes.Count().ShouldBe(2, "one package node + one project node");
        graph.Edges.Count().ShouldBe(1, "one edge: project → package");
        graph.Conflicts.ShouldBeEmpty();
    }

    [Fact]
    public void Build_SamePackageDifferentVersions_DetectsConflict()
    {
        var packages = new List<PackageReference>
        {
            new() { PackageName = "Serilog", Version = "3.0.0", ProjectFile = "/src/App.csproj" },
            new() { PackageName = "Serilog", Version = "4.0.0", ProjectFile = "/src/Api.csproj" }
        };

        var graph = _sut.Build(packages, new DependencyGraphOptions { HighlightConflicts = true });

        graph.Conflicts.ShouldContainKey("Serilog");
        var conflict = graph.Conflicts["Serilog"];
        conflict.Versions.Count().ShouldBe(2);
        conflict.Severity.ShouldBe(3, "major version difference → severity 3");

        var conflictNodes = graph.Nodes.Where(n => n.HasConflict).ToList();
        conflictNodes.Count().ShouldBe(2);
    }

    [Fact]
    public void Build_SamePackageSameVersion_NoDuplicateNodes()
    {
        // Same package referenced in two projects at the same version
        var packages = new List<PackageReference>
        {
            new() { PackageName = "FluentAssertions", Version = "7.0.0", ProjectFile = "/src/App.csproj" },
            new() { PackageName = "FluentAssertions", Version = "7.0.0", ProjectFile = "/src/Lib.csproj" }
        };

        var graph = _sut.Build(packages);

        // One package node (FA 7.0.0), two project nodes
        var pkgNodes = graph.Nodes.Where(n => n.Type == NodeType.RootPackage
                                           && n.PackageId == "FluentAssertions").ToList();
        pkgNodes.Count().ShouldBe(1, "same version should only produce one package node");
        graph.Conflicts.ShouldBeEmpty();
    }

    [Fact]
    public void Build_PatchVersionDiff_LowSeverity()
    {
        var packages = new List<PackageReference>
        {
            new() { PackageName = "Moq", Version = "4.20.0", ProjectFile = "/src/App.csproj" },
            new() { PackageName = "Moq", Version = "4.20.72", ProjectFile = "/src/Tests.csproj" }
        };

        var graph = _sut.Build(packages, new DependencyGraphOptions { HighlightConflicts = true });

        graph.Conflicts["Moq"].Severity.ShouldBe(1, "only patch differs → low severity");
    }

    [Fact]
    public void Build_MinorVersionDiff_MediumSeverity()
    {
        var packages = new List<PackageReference>
        {
            new() { PackageName = "MediatR", Version = "12.1.0", ProjectFile = "/src/App.csproj" },
            new() { PackageName = "MediatR", Version = "12.4.0", ProjectFile = "/src/Api.csproj" }
        };

        var graph = _sut.Build(packages, new DependencyGraphOptions { HighlightConflicts = true });

        graph.Conflicts["MediatR"].Severity.ShouldBe(2, "minor version differs → medium severity");
    }

    [Fact]
    public void Build_ConflictDisabled_NoConflictEdges()
    {
        var packages = new List<PackageReference>
        {
            new() { PackageName = "AutoMapper", Version = "12.0.0", ProjectFile = "/a.csproj" },
            new() { PackageName = "AutoMapper", Version = "13.0.0", ProjectFile = "/b.csproj" }
        };

        var graph = _sut.Build(packages, new DependencyGraphOptions { HighlightConflicts = false });

        graph.Conflicts.ShouldBeEmpty();
        graph.Edges.ShouldNotContain(e => e.IsConflict);
    }
}
