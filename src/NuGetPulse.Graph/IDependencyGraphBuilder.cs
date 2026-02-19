using NuGetPulse.Core.Models;
using NuGetPulse.Graph.Models;

namespace NuGetPulse.Graph;

/// <summary>
/// Builds a <see cref="DependencyGraph"/> from a flat list of <see cref="PackageReference"/>
/// objects returned by <see cref="NuGetPulse.Core.Abstractions.IPackageScanner"/>.
///
/// The graph groups packages by name, detects version conflicts across project files,
/// and represents project-to-package relationships as edges.
/// </summary>
public interface IDependencyGraphBuilder
{
    /// <summary>
    /// Build a dependency graph from the provided package references.
    /// </summary>
    DependencyGraph Build(
        IReadOnlyList<PackageReference> packages,
        DependencyGraphOptions? options = null);
}
