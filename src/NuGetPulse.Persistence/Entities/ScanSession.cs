namespace NuGetPulse.Persistence.Entities;

/// <summary>
/// A scan session represents one invocation of PackageScanner against a directory or solution.
/// Ported and modernised from NugetManager's Repository + scan tracking concepts.
/// </summary>
public sealed class ScanSession
{
    public int Id { get; set; }

    /// <summary>Human-readable name for this repository / project root.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the directory or solution file that was scanned.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the scan was started.</summary>
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Duration of the scan in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>Number of packages found in this scan.</summary>
    public int PackageCount { get; set; }

    /// <summary>Number of vulnerabilities detected (OSV).</summary>
    public int VulnerabilityCount { get; set; }

    /// <summary>Status of the scan.</summary>
    public ScanSessionStatus Status { get; set; } = ScanSessionStatus.Completed;

    /// <summary>Error message if the scan failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Packages found in this scan.</summary>
    public List<ScannedPackage> Packages { get; set; } = [];
}

public enum ScanSessionStatus
{
    InProgress,
    Completed,
    Failed
}
