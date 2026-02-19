using NuGetPulse.Core.Models;

namespace NuGetPulse.Persistence.Entities;

/// <summary>
/// A NuGet package reference found during a scan session.
/// Mirrors NuGetPulse.Core.Models.PackageReference but with EF Core persistence support.
/// </summary>
public sealed class ScannedPackage
{
    public int Id { get; set; }

    public int ScanSessionId { get; set; }
    public ScanSession ScanSession { get; set; } = null!;

    public string PackageName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string ProjectFile { get; set; } = string.Empty;
    public PackageType Type { get; set; }
    public bool IsCentrallyManaged { get; set; }
    public string? VersionOverride { get; set; }
    public string? SourceFile { get; set; }
    public PackageSourceType SourceType { get; set; }

    /// <summary>Number of known vulnerabilities at scan time (0 if not checked).</summary>
    public int VulnerabilityCount { get; set; }
}
