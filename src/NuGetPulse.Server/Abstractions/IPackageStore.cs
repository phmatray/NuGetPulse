using NuGetPulse.Server.Models;

namespace NuGetPulse.Server.Abstractions;

/// <summary>Abstraction over the local NuGet package storage.</summary>
public interface IPackageStore
{
    /// <summary>Returns all packages available on the server.</summary>
    Task<IReadOnlyList<PackageSummary>> ListAsync(CancellationToken ct = default);

    /// <summary>Returns details for a specific package/version.</summary>
    Task<PackageDetails?> GetDetailsAsync(string packageId, string version, CancellationToken ct = default);

    /// <summary>Returns the .nupkg file path for download (null if not found).</summary>
    Task<string?> GetNupkgPathAsync(string packageId, string version, CancellationToken ct = default);

    /// <summary>Pushes a new .nupkg file into the store.</summary>
    Task PushAsync(Stream nupkgStream, CancellationToken ct = default);
}
