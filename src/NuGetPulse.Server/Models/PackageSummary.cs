namespace NuGetPulse.Server.Models;

/// <summary>Summary of a package available on the self-hosted NuGet server.</summary>
public sealed class PackageSummary
{
    public string Id { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Authors { get; init; }
    public DateTime? Published { get; init; }
}

/// <summary>Detailed information about a package on the self-hosted NuGet server.</summary>
public sealed class PackageDetails
{
    public string Id { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Authors { get; init; }
    public string? ProjectUrl { get; init; }
    public string? LicenseExpression { get; init; }
    public string? Tags { get; init; }
    public DateTime? Published { get; init; }
    public long FileSizeBytes { get; init; }
    public string? NupkgPath { get; init; }
}
