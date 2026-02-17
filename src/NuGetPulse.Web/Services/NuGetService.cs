using System.Text.Json;
using System.Text.Json.Serialization;
using NuGetPulse.Web.Models;

namespace NuGetPulse.Web.Services;

public sealed class NuGetService(HttpClient httpClient, ILogger<NuGetService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string SearchBaseUrl = "https://azuresearch-usnc.nuget.org/query";
    private const string RegistrationBaseUrl = "https://api.nuget.org/v3/registration5-semver1";
    private const string FlatContainerBaseUrl = "https://api.nuget.org/v3-flatcontainer";

    // ─── Search ───────────────────────────────────────────────────────────────

    public async Task<List<PackageSearchResult>> SearchAsync(string query, int take = 8, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var url = $"{SearchBaseUrl}?q={Uri.EscapeDataString(query)}&take={take}&prerelease=false";
        try
        {
            var response = await httpClient.GetFromJsonAsync<NuGetSearchResponse>(url, JsonOptions, ct);
            return response?.Data?.Select(d => new PackageSearchResult(
                d.Id,
                d.Version,
                d.TotalDownloads,
                d.Description,
                d.IconUrl
            )).ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "NuGet search failed for query '{Query}'", query);
            return [];
        }
    }

    // ─── Package detail ────────────────────────────────────────────────────────

    public async Task<PackageStats?> GetPackageStatsAsync(string packageId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(packageId)) return null;

        var id = packageId.ToLowerInvariant();

        // 1) Search for the package (gives us totalDownloads + per-version downloads)
        var searchUrl = $"{SearchBaseUrl}?q=packageid:{Uri.EscapeDataString(packageId)}&take=1&prerelease=false";

        NuGetSearchData? searchData = null;
        try
        {
            var searchResp = await httpClient.GetFromJsonAsync<NuGetSearchResponse>(searchUrl, JsonOptions, ct);
            searchData = searchResp?.Data?.FirstOrDefault(d =>
                string.Equals(d.Id, packageId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Search failed for package '{Package}'", packageId);
        }

        if (searchData is null) return null;

        // 2) Registration for detailed metadata
        RegistrationCatalogEntry? catalogEntry = null;
        try
        {
            catalogEntry = await GetLatestCatalogEntryAsync(id, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Registration fetch failed for '{Package}'", packageId);
        }

        // 3) Build per-version list
        var versions = searchData.Versions?
            .Select(v => new VersionDownload(v.Version, v.Downloads))
            .ToList() ?? [];

        // Normalize bar widths relative to the highest version
        if (versions.Count > 0)
        {
            long maxDl = versions.Max(v => v.Downloads);
            if (maxDl > 0)
                foreach (var v in versions)
                    v.BarWidthPercent = (int)(v.Downloads * 100 / maxDl);
        }

        // 4) Resolve target frameworks from registration
        var frameworks = catalogEntry?.DependencyGroups?
            .Select(g => NormalizeFramework(g.TargetFramework))
            .Where(f => !string.IsNullOrEmpty(f))
            .Distinct()
            .OrderBy(f => f)
            .ToList<string>() ?? [];

        return new PackageStats
        {
            Id = searchData.Id,
            Version = searchData.Version,
            Description = searchData.Description,
            Authors = searchData.Authors != null ? string.Join(", ", searchData.Authors) : null,
            IconUrl = searchData.IconUrl,
            ProjectUrl = catalogEntry?.ProjectUrl ?? searchData.ProjectUrl,
            LicenseExpression = catalogEntry?.LicenseExpression,
            TotalDownloads = searchData.TotalDownloads,
            Published = catalogEntry?.Published,
            IsVerified = searchData.Verified,
            Versions = versions,
            Tags = searchData.Tags ?? [],
            TargetFrameworks = frameworks
        };
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

    private async Task<RegistrationCatalogEntry?> GetLatestCatalogEntryAsync(string id, CancellationToken ct)
    {
        var url = $"{RegistrationBaseUrl}/{id}/index.json";
        var index = await httpClient.GetFromJsonAsync<RegistrationIndex>(url, JsonOptions, ct);

        // Each page in items may be an array of leaves; find the latest (last page, last item)
        var lastPage = index?.Items?.LastOrDefault();
        if (lastPage?.Items is { Count: > 0 } leaves)
            return leaves.LastOrDefault()?.CatalogEntry;

        // If items are paged (URL references), follow the last one
        if (lastPage?.Url is { } pageUrl)
        {
            var page = await httpClient.GetFromJsonAsync<RegistrationPage>(pageUrl, JsonOptions, ct);
            return page?.Items?.LastOrDefault()?.CatalogEntry;
        }

        return null;
    }

    private static string NormalizeFramework(string? tfm)
    {
        if (string.IsNullOrEmpty(tfm)) return string.Empty;
        return tfm.ToLowerInvariant() switch
        {
            var t when t.StartsWith(".netcoreapp") => t.Replace(".netcoreapp", "netcoreapp"),
            var t when t.StartsWith(".netstandard") => t.Replace(".netstandard", "netstandard"),
            var t when t.StartsWith(".netframework") => t.Replace(".netframework", "net"),
            var t when t.StartsWith(".net") => t.Replace(".net", "net"),
            var t => t
        };
    }

    // ─── JSON DTOs ─────────────────────────────────────────────────────────────

    private sealed class NuGetSearchResponse
    {
        public int TotalHits { get; init; }
        public List<NuGetSearchData>? Data { get; init; }
    }

    private sealed class NuGetSearchData
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("totalDownloads")]
        public long TotalDownloads { get; init; }

        [JsonPropertyName("verified")]
        public bool Verified { get; init; }

        [JsonPropertyName("authors")]
        public List<string>? Authors { get; init; }

        [JsonPropertyName("iconUrl")]
        public string? IconUrl { get; init; }

        [JsonPropertyName("projectUrl")]
        public string? ProjectUrl { get; init; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; init; }

        [JsonPropertyName("versions")]
        public List<VersionSummary>? Versions { get; init; }
    }

    private sealed class VersionSummary
    {
        [JsonPropertyName("version")]
        public string Version { get; init; } = string.Empty;

        [JsonPropertyName("downloads")]
        public long Downloads { get; init; }
    }

    private sealed class RegistrationIndex
    {
        [JsonPropertyName("items")]
        public List<RegistrationPage>? Items { get; init; }
    }

    private sealed class RegistrationPage
    {
        [JsonPropertyName("@id")]
        public string? Url { get; init; }

        [JsonPropertyName("items")]
        public List<RegistrationLeaf>? Items { get; init; }
    }

    private sealed class RegistrationLeaf
    {
        [JsonPropertyName("catalogEntry")]
        public RegistrationCatalogEntry? CatalogEntry { get; init; }
    }

    private sealed class RegistrationCatalogEntry
    {
        [JsonPropertyName("licenseExpression")]
        public string? LicenseExpression { get; init; }

        [JsonPropertyName("projectUrl")]
        public string? ProjectUrl { get; init; }

        [JsonPropertyName("published")]
        public DateTime? Published { get; init; }

        [JsonPropertyName("dependencyGroups")]
        public List<DependencyGroup>? DependencyGroups { get; init; }
    }

    private sealed class DependencyGroup
    {
        [JsonPropertyName("targetFramework")]
        public string? TargetFramework { get; init; }
    }
}
