namespace NuGetPulse.Core.Models;

/// <summary>Composite health score for a NuGet package (0–100).</summary>
public sealed class HealthScore
{
    /// <summary>Overall score 0–100.</summary>
    public int Score { get; init; }

    /// <summary>Downloads score component (weight 30%).</summary>
    public int DownloadsScore { get; init; }

    /// <summary>Freshness score component (weight 30%).</summary>
    public int FreshnessScore { get; init; }

    /// <summary>Vulnerability score component (weight 25%).</summary>
    public int VulnerabilityScore { get; init; }

    /// <summary>Deprecation score component (weight 15%).</summary>
    public int DeprecationScore { get; init; }

    /// <summary>Number of known vulnerabilities (from OSV).</summary>
    public int VulnerabilityCount { get; init; }

    /// <summary>Whether the package is deprecated.</summary>
    public bool IsDeprecated { get; init; }

    public HealthStatus Status => Score switch
    {
        >= 80 => HealthStatus.Healthy,
        >= 60 => HealthStatus.Warning,
        _ => HealthStatus.Critical
    };

    /// <summary>Compute a HealthScore from raw metrics.</summary>
    public static HealthScore Compute(
        long totalDownloads,
        DateTime? lastPublished,
        int vulnerabilityCount,
        bool isDeprecated)
    {
        // Downloads: normalised log-scale; 10M+ = 100
        var dlScore = totalDownloads switch
        {
            >= 10_000_000 => 100,
            >= 1_000_000 => 80,
            >= 100_000 => 60,
            >= 10_000 => 40,
            >= 1_000 => 20,
            _ => 5
        };

        // Freshness: days since last publish
        var freshnessScore = lastPublished.HasValue
            ? (DateTime.UtcNow - lastPublished.Value).TotalDays switch
            {
                <= 30 => 100,
                <= 90 => 85,
                <= 180 => 70,
                <= 365 => 50,
                <= 730 => 30,
                _ => 10
            }
            : 50; // unknown → neutral

        // Vulnerabilities: each vuln costs 25 points
        var vulnScore = Math.Max(0, 100 - vulnerabilityCount * 25);

        // Deprecation
        var depScore = isDeprecated ? 0 : 100;

        var composite = (int)Math.Round(
            dlScore * 0.30 +
            freshnessScore * 0.30 +
            vulnScore * 0.25 +
            depScore * 0.15);

        return new HealthScore
        {
            Score = composite,
            DownloadsScore = dlScore,
            FreshnessScore = (int)freshnessScore,
            VulnerabilityScore = vulnScore,
            DeprecationScore = depScore,
            VulnerabilityCount = vulnerabilityCount,
            IsDeprecated = isDeprecated
        };
    }
}

public enum HealthStatus { Healthy, Warning, Critical }
