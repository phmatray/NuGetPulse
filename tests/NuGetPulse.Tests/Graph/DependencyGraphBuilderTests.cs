using FluentAssertions;
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

        graph.Nodes.Should().BeEmpty();
        graph.Edges.Should().BeEmpty();
        graph.Conflicts.Should().BeEmpty();
    }

    [Fact]
    public void Build_SinglePackageSingleProject_CreatesPackageAndProjectNodes()
    {
        var packages = new List<PackageReference>
        {
            new() { PackageName = "Newtonsoft.Json", Version = "13.0.3", ProjectFile = "/src/App/App.csproj" }
        };

        var graph = _sut.Build(packages);

        graph.Nodes.Should().HaveCount(2, "one package node + one project node");
        graph.Edges.Should().HaveCount(1, "one edge: project → package");
        graph.Conflicts.Should().BeEmpty();
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

        graph.Conflicts.Should().ContainKey("Serilog");
        var conflict = graph.Conflicts["Serilog"];
        conflict.Versions.Should().HaveCount(2);
        conflict.Severity.Should().Be(3, "major version difference → severity 3");

        var conflictNodes = graph.Nodes.Where(n => n.HasConflict).ToList();
        conflictNodes.Should().HaveCount(2);
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
        pkgNodes.Should().HaveCount(1, "same version should only produce one package node");
        graph.Conflicts.Should().BeEmpty("same version means no conflict");
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

        graph.Conflicts["Moq"].Severity.Should().Be(1, "only patch differs → low severity");
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

        graph.Conflicts["MediatR"].Severity.Should().Be(2, "minor version differs → medium severity");
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

        graph.Conflicts.Should().BeEmpty();
        graph.Edges.Should().NotContain(e => e.IsConflict);
    }
}
