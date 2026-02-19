using NuGetPulse.Core.Models;

namespace NuGetPulse.Core.Abstractions;

/// <summary>Scans project files and extracts NuGet package references.</summary>
public interface IPackageScanner
{
    /// <summary>Extract package references from a .csproj or .fsproj file.</summary>
    Task<IReadOnlyList<PackageReference>> ScanProjectFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Extract package references from a packages.config file.</summary>
    Task<IReadOnlyList<PackageReference>> ScanPackagesConfigAsync(string filePath, CancellationToken ct = default);

    /// <summary>Extract package versions from a Directory.Packages.props file.</summary>
    Task<IReadOnlyList<PackageReference>> ScanDirectoryPackagesPropsAsync(string filePath, CancellationToken ct = default);

    /// <summary>Recursively scan a directory and return all package references found.</summary>
    Task<IReadOnlyList<PackageReference>> ScanDirectoryAsync(string directoryPath, CancellationToken ct = default);
}
