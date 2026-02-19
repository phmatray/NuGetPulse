namespace NuGetPulse.Core.Models;

/// <summary>Source type of a package reference (from NugetManager).</summary>
public enum PackageSourceType
{
    ProjectFile,
    PackagesConfig,
    DirectoryPackagesProps,
    DirectoryBuildProps,
    DirectoryBuildTargets
}

/// <summary>Type of package dependency reference.</summary>
public enum PackageType
{
    PackageReference,
    PackagesConfig,
    ProjectReference
}

/// <summary>Represents a NuGet package reference found in a project file.</summary>
public sealed class PackageReference
{
    public string PackageName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string ProjectFile { get; set; } = string.Empty;
    public PackageType Type { get; set; }
    public bool IsCentrallyManaged { get; set; }
    public string? VersionOverride { get; set; }
    public string? SourceFile { get; set; }
    public PackageSourceType SourceType { get; set; }
    public int? RepositoryId { get; set; }
}
