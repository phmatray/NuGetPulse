using FluentAssertions;
using NuGetPulse.Core.Models;

namespace NuGetPulse.Tests.Core;

/// <summary>Unit tests for the <see cref="HealthScore"/> computation logic.</summary>
public sealed class HealthScoreTests
{
    [Theory]
    [InlineData(10_000_000, 0, 0, false, HealthStatus.Healthy)]
    [InlineData(100, 0, 5, true, HealthStatus.Critical)]
    public void Compute_GivenInputs_ProducesExpectedStatus(
        long downloads, int daysOld, int vulns, bool deprecated, HealthStatus expected)
    {
        var published = daysOld == 0 ? DateTime.UtcNow : DateTime.UtcNow.AddDays(-daysOld);
        var score = HealthScore.Compute(downloads, published, vulns, deprecated);
        score.Status.Should().Be(expected);
    }

    [Fact]
    public void Compute_NoVulnerabilities_VulnScoreIs100()
    {
        var score = HealthScore.Compute(1_000_000, DateTime.UtcNow, 0, false);
        score.VulnerabilityScore.Should().Be(100);
    }

    [Fact]
    public void Compute_FourVulnerabilities_VulnScoreIsZero()
    {
        var score = HealthScore.Compute(1_000_000, DateTime.UtcNow, 4, false);
        score.VulnerabilityScore.Should().Be(0);
    }

    [Fact]
    public void Compute_Deprecated_DeprecationScoreIsZero()
    {
        var score = HealthScore.Compute(1_000_000, DateTime.UtcNow, 0, true);
        score.DeprecationScore.Should().Be(0);
    }

    [Fact]
    public void Compute_ScoreIsCappedAt100()
    {
        var score = HealthScore.Compute(100_000_000, DateTime.UtcNow, 0, false);
        score.Score.Should().BeLessOrEqualTo(100);
    }

    [Fact]
    public void Compute_NullPublished_ReturnsNeutralFreshnessScore()
    {
        var score = HealthScore.Compute(1_000_000, null, 0, false);
        score.FreshnessScore.Should().Be(50);
    }
}
