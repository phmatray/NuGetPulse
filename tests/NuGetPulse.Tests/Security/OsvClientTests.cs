using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NuGetPulse.Core.Models;
using NuGetPulse.Security;

namespace NuGetPulse.Tests.Security;

/// <summary>
/// Unit tests for <see cref="OsvClient"/> using a custom <see cref="HttpMessageHandler"/>
/// to avoid real network calls (OSV API).
/// </summary>
public sealed class OsvClientTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Build an OsvClient with a fake HTTP handler.</summary>
    private static OsvClient CreateClient(string responseBody, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpHandler(status, responseBody);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.osv.dev/") };
        return new OsvClient(httpClient, NullLogger<OsvClient>.Instance);
    }

    private static string EmptyResponse => """{"vulns":[]}""";

    private static string SingleHighVulnResponse => """
        {
          "vulns": [
            {
              "id": "GHSA-1234-5678-abcd",
              "summary": "Remote code execution in Foo",
              "details": "An attacker can exploit this vulnerability.",
              "aliases": ["CVE-2024-12345"],
              "published": "2024-01-15T00:00:00Z",
              "modified": "2024-02-01T00:00:00Z",
              "database_specific": { "severity": "HIGH" },
              "references": [{ "type": "WEB", "url": "https://example.com/advisory" }]
            }
          ]
        }
        """;

    private static string MultipleSeverityResponse => """
        {
          "vulns": [
            {
              "id": "GHSA-critical-001",
              "summary": "Critical vuln",
              "database_specific": { "severity": "CRITICAL" }
            },
            {
              "id": "GHSA-medium-002",
              "summary": "Medium vuln",
              "database_specific": { "severity": "MEDIUM" }
            },
            {
              "id": "GHSA-low-003",
              "summary": "Low severity issue",
              "database_specific": { "severity": "LOW" }
            }
          ]
        }
        """;

    // ─── ScanAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ScanAsync_EmptyResponse_ReturnsReportWithNoVulns()
    {
        var client = CreateClient(EmptyResponse);

        var report = await client.ScanAsync("Newtonsoft.Json", "13.0.3");

        report.PackageId.Should().Be("Newtonsoft.Json");
        report.Version.Should().Be("13.0.3");
        report.HasVulnerabilities.Should().BeFalse();
        report.Count.Should().Be(0);
    }

    [Fact]
    public async Task ScanAsync_OneHighVuln_ReturnsReportWithVuln()
    {
        var client = CreateClient(SingleHighVulnResponse);

        var report = await client.ScanAsync("VulnerablePackage", "1.0.0");

        report.HasVulnerabilities.Should().BeTrue();
        report.Count.Should().Be(1);
        report.Vulnerabilities[0].Id.Should().Be("GHSA-1234-5678-abcd");
        report.Vulnerabilities[0].Severity.Should().Be(OsvSeverity.High);
        report.Vulnerabilities[0].Summary.Should().Be("Remote code execution in Foo");
        report.Vulnerabilities[0].Aliases.Should().Contain("CVE-2024-12345");
        report.Vulnerabilities[0].ReferenceUrl.Should().Be("https://example.com/advisory");
    }

    [Fact]
    public async Task ScanAsync_MultipleVulns_ParsesAllSeverities()
    {
        var client = CreateClient(MultipleSeverityResponse);

        var report = await client.ScanAsync("BadPackage", "0.1.0");

        report.Count.Should().Be(3);
        report.Vulnerabilities.Should().Contain(v => v.Severity == OsvSeverity.Critical);
        report.Vulnerabilities.Should().Contain(v => v.Severity == OsvSeverity.Medium);
        report.Vulnerabilities.Should().Contain(v => v.Severity == OsvSeverity.Low);
    }

    [Fact]
    public async Task ScanAsync_ApiReturnsError_ReturnsEmptyReport()
    {
        // OSV API returns 500 → OsvClient should gracefully return empty report
        var client = CreateClient("Internal Server Error", HttpStatusCode.InternalServerError);

        var report = await client.ScanAsync("SomePackage", "1.0.0");

        // Should not throw; returns empty report (best-effort)
        report.HasVulnerabilities.Should().BeFalse();
        report.PackageId.Should().Be("SomePackage");
    }

    [Fact]
    public async Task ScanAsync_MalformedJson_ReturnsEmptyReport()
    {
        var client = CreateClient("not json at all !!!", HttpStatusCode.OK);

        var report = await client.ScanAsync("SomePackage", "2.0.0");

        report.HasVulnerabilities.Should().BeFalse();
    }

    [Fact]
    public async Task ScanAsync_NullVulnsArray_ReturnsEmptyReport()
    {
        var client = CreateClient("""{"vulns":null}""");

        var report = await client.ScanAsync("SomePackage", "1.0.0");

        report.HasVulnerabilities.Should().BeFalse();
        report.Vulnerabilities.Should().BeEmpty();
    }

    // ─── ScanBatchAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ScanBatchAsync_MultiplePackages_ReturnsOneReportPerPackage()
    {
        var client = CreateClient(EmptyResponse);

        var packages = new List<(string, string)>
        {
            ("Newtonsoft.Json", "13.0.3"),
            ("Serilog", "4.0.0"),
            ("FluentAssertions", "7.0.0")
        };

        var reports = await client.ScanBatchAsync(packages);

        reports.Should().HaveCount(3);
        reports.Should().AllSatisfy(r => r.HasVulnerabilities.Should().BeFalse());
    }

    [Fact]
    public async Task ScanBatchAsync_EmptyInput_ReturnsEmptyList()
    {
        var client = CreateClient(EmptyResponse);

        var reports = await client.ScanBatchAsync([]);

        reports.Should().BeEmpty();
    }

    // ─── Severity parsing edge cases ──────────────────────────────────────────

    [Theory]
    [InlineData("CRITICAL", OsvSeverity.Critical)]
    [InlineData("HIGH", OsvSeverity.High)]
    [InlineData("MEDIUM", OsvSeverity.Medium)]
    [InlineData("LOW", OsvSeverity.Low)]
    [InlineData("UNKNOWN", OsvSeverity.Unknown)]
    [InlineData(null, OsvSeverity.Unknown)]
    [InlineData("bogus", OsvSeverity.Unknown)]
    public async Task ScanAsync_DatabaseSpecificSeverity_ParsedCorrectly(string? severity, OsvSeverity expected)
    {
        string json;
        if (severity is null)
            json = """{"vulns":[{"id":"TEST","summary":"test","database_specific":{}}]}""";
        else
            json = $"{{\"vulns\":[{{\"id\":\"TEST\",\"summary\":\"test\",\"database_specific\":{{\"severity\":\"{severity}\"}}}}]}}";

        var client = CreateClient(json);
        var report = await client.ScanAsync("Pkg", "1.0.0");

        report.Vulnerabilities.Should().HaveCount(1);
        report.Vulnerabilities[0].Severity.Should().Be(expected);
    }

    // ─── Fake HTTP handler ────────────────────────────────────────────────────

    private sealed class FakeHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
