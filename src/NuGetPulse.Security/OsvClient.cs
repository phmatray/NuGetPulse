using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NuGetPulse.Core.Abstractions;
using NuGetPulse.Core.Models;

namespace NuGetPulse.Security;

/// <summary>
/// OSV (Open Source Vulnerabilities) scanner for NuGet packages.
/// Queries https://api.osv.dev/v1/query for each package/version pair.
/// Implements the NugetOSV concept with a real, production-ready implementation.
/// </summary>
public sealed class OsvClient(HttpClient httpClient, ILogger<OsvClient> logger) : IVulnerabilityScanner
{
    private const string OsvApiBase = "https://api.osv.dev/v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ─── Single package scan ──────────────────────────────────────────────────

    public async Task<VulnerabilityReport> ScanAsync(string packageId, string version, CancellationToken ct = default)
    {
        var request = new OsvQueryRequest
        {
            Version = version,
            Package = new OsvPackage { Name = packageId, Ecosystem = "NuGet" }
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync($"{OsvApiBase}/query", request, JsonOptions, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OsvQueryResponse>(JsonOptions, ct);
            var vulns = MapVulnerabilities(result?.Vulns);

            return new VulnerabilityReport
            {
                PackageId = packageId,
                Version = version,
                Vulnerabilities = vulns
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "OSV scan failed for {PackageId}@{Version}", packageId, version);
            return new VulnerabilityReport { PackageId = packageId, Version = version };
        }
    }

    // ─── Batch scan ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<VulnerabilityReport>> ScanBatchAsync(
        IEnumerable<(string packageId, string version)> packages,
        CancellationToken ct = default)
    {
        var tasks = packages.Select(p => ScanAsync(p.packageId, p.version, ct));
        var results = await Task.WhenAll(tasks);
        return results;
    }

    // ─── Mapping ──────────────────────────────────────────────────────────────

    private static List<OsvVulnerability> MapVulnerabilities(List<OsvVulnDto>? dtos)
    {
        if (dtos is null or { Count: 0 }) return [];

        return dtos.Select(dto => new OsvVulnerability
        {
            Id = dto.Id ?? string.Empty,
            Summary = dto.Summary ?? string.Empty,
            Details = dto.Details,
            Severity = ParseSeverity(dto.DatabaseSpecific?.Severity ?? dto.Severity?.FirstOrDefault()?.Score),
            CvssScore = dto.Severity?.FirstOrDefault()?.Score,
            Aliases = dto.Aliases ?? [],
            ReferenceUrl = dto.References?.FirstOrDefault()?.Url,
            Published = dto.Published,
            Modified = dto.Modified
        }).ToList();
    }

    private static OsvSeverity ParseSeverity(string? score) =>
        score?.ToUpperInvariant() switch
        {
            "CRITICAL" => OsvSeverity.Critical,
            "HIGH" => OsvSeverity.High,
            "MEDIUM" => OsvSeverity.Medium,
            "LOW" => OsvSeverity.Low,
            _ => OsvSeverity.Unknown
        };

    // ─── DTOs ─────────────────────────────────────────────────────────────────

    private sealed record OsvQueryRequest
    {
        [JsonPropertyName("version")]
        public string? Version { get; init; }

        [JsonPropertyName("package")]
        public OsvPackage? Package { get; init; }
    }

    private sealed record OsvPackage
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("ecosystem")]
        public string Ecosystem { get; init; } = "NuGet";
    }

    private sealed record OsvQueryResponse
    {
        [JsonPropertyName("vulns")]
        public List<OsvVulnDto>? Vulns { get; init; }
    }

    private sealed record OsvVulnDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("summary")]
        public string? Summary { get; init; }

        [JsonPropertyName("details")]
        public string? Details { get; init; }

        [JsonPropertyName("aliases")]
        public List<string>? Aliases { get; init; }

        [JsonPropertyName("published")]
        public DateTime? Published { get; init; }

        [JsonPropertyName("modified")]
        public DateTime? Modified { get; init; }

        [JsonPropertyName("severity")]
        public List<OsvSeverityDto>? Severity { get; init; }

        [JsonPropertyName("references")]
        public List<OsvReferenceDto>? References { get; init; }

        [JsonPropertyName("database_specific")]
        public OsvDatabaseSpecific? DatabaseSpecific { get; init; }
    }

    private sealed record OsvSeverityDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("score")]
        public string? Score { get; init; }
    }

    private sealed record OsvReferenceDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }

    private sealed record OsvDatabaseSpecific
    {
        [JsonPropertyName("severity")]
        public string? Severity { get; init; }
    }
}
