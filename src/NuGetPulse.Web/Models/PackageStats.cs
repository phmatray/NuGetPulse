namespace NuGetPulse.Web.Models;

/// <summary>Summary of a NuGet package's health and download statistics.</summary>
public sealed record PackageStats
{
    public required string Id { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? Authors { get; init; }
    public string? IconUrl { get; init; }
    public string? ProjectUrl { get; init; }
    public string? LicenseExpression { get; init; }
    public long TotalDownloads { get; init; }
    public DateTime? Published { get; init; }
    public bool IsVerified { get; init; }
    public List<VersionDownload> Versions { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public List<string> TargetFrameworks { get; init; } = [];

    public int HealthScore => CalculateHealthScore();

    public string HealthLabel => HealthScore switch
    {
        >= 80 => "Excellent",
        >= 60 => "Good",
        >= 40 => "Fair",
        _ => "Needs Attention"
    };

    public string HealthBadgeClass => HealthScore switch
    {
        >= 80 => "badge bg-success",
        >= 60 => "badge bg-primary",
        >= 40 => "badge bg-warning text-dark",
        _ => "badge bg-danger"
    };

    public string HealthBarClass => HealthScore switch
    {
        >= 80 => "bg-success",
        >= 60 => "bg-primary",
        >= 40 => "bg-warning",
        _ => "bg-danger"
    };

    public string FormattedDownloads => TotalDownloads switch
    {
        >= 1_000_000 => $"{TotalDownloads / 1_000_000.0:F1}M",
        >= 1_000     => $"{TotalDownloads / 1_000.0:F1}K",
        _            => TotalDownloads.ToString()
    };

    /// <summary>Top 5 versions by download count, descending.</summary>
    public IEnumerable<VersionDownload> TopVersions =>
        Versions.OrderByDescending(v => v.Downloads).Take(5);

    /// <summary>Last 6 versions by semantic version order, for trend display.</summary>
    public IEnumerable<VersionDownload> RecentVersions =>
        Versions.TakeLast(6);

    private int CalculateHealthScore()
    {
        int score = 0;

        // Downloads: up to 40 points
        score += TotalDownloads switch
        {
            >= 1_000_000 => 40,
            >= 100_000   => 30,
            >= 10_000    => 20,
            >= 1_000     => 10,
            _            => 5
        };

        // Version count: up to 20 points (active maintenance)
        score += Versions.Count switch
        {
            >= 20 => 20,
            >= 10 => 15,
            >= 5  => 10,
            >= 2  => 5,
            _     => 0
        };

        // Recent activity: up to 20 points
        if (Published.HasValue)
        {
            var monthsAgo = (DateTime.UtcNow - Published.Value).TotalDays / 30;
            score += monthsAgo switch
            {
                <= 3  => 20,
                <= 6  => 15,
                <= 12 => 10,
                <= 24 => 5,
                _     => 0
            };
        }

        // Has project URL: 10 points
        if (!string.IsNullOrEmpty(ProjectUrl)) score += 10;

        // Has license: 10 points
        if (!string.IsNullOrEmpty(LicenseExpression)) score += 10;

        return Math.Min(score, 100);
    }
}

public sealed record VersionDownload(string Version, long Downloads)
{
    public string FormattedDownloads => Downloads switch
    {
        >= 1_000_000 => $"{Downloads / 1_000_000.0:F1}M",
        >= 1_000     => $"{Downloads / 1_000.0:F1}K",
        _            => Downloads.ToString()
    };

    /// <summary>Bar width as a % relative to some max (set externally).</summary>
    public int BarWidthPercent { get; set; }
}

public sealed record PackageSearchResult(
    string Id,
    string Version,
    long TotalDownloads,
    string? Description,
    string? IconUrl
);
