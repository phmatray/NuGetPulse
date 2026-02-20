namespace NuGetPulse.Tests.Web;

using Microsoft.Extensions.Logging.Abstractions;
using NuGetPulse.Web.Services;

/// <summary>
/// Integration tests for NuGetService against real NuGet.org API.
/// These tests verify that critical packages can be searched and retrieved.
/// </summary>
public class NuGetServiceIntegrationTests
{
    private readonly NuGetService _service;

    public NuGetServiceIntegrationTests()
    {
        var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "NuGetPulse/1.0 (+https://nugetpulse.garry-ai.cloud)");

        _service = new NuGetService(httpClient, new NullLogger<NuGetService>());
    }

    [Fact]
    public async Task SearchAsync_Mutty_ReturnsResults()
    {
        // Arrange
        const string query = "Mutty";

        // Act
        var results = await _service.SearchAsync(query);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.Id.Equals("Mutty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetPackageStatsAsync_Mutty_ReturnsStats()
    {
        // Arrange
        const string packageId = "Mutty";

        // Act
        var stats = await _service.GetPackageStatsAsync(packageId);

        // Assert
        stats.Should().NotBeNull();
        stats!.Id.Should().Be("Mutty");
        stats.Version.Should().NotBeNullOrEmpty();
        stats.TotalDownloads.Should().BeGreaterThan(0);
        stats.Authors.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("Newtonsoft.Json")]
    [InlineData("Serilog")]
    [InlineData("MediatR")]
    public async Task SearchAsync_PopularPackages_ReturnsResults(string packageName)
    {
        // Act
        var results = await _service.SearchAsync(packageName);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(r => r.Id.Equals(packageName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
    {
        // Act
        var results = await _service.SearchAsync("");

        // Assert
        results.Should().BeEmpty();
    }
}
