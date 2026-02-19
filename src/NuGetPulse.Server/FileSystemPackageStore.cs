using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NuGetPulse.Server.Abstractions;
using NuGetPulse.Server.Models;

namespace NuGetPulse.Server;

/// <summary>
/// File-system backed NuGet package store.
/// Packages are stored as .nupkg files under <see cref="PackageStoreOptions.RootPath"/>.
/// Inspired by NugetServer's file-system infrastructure approach.
/// </summary>
public sealed class FileSystemPackageStore(
    IOptions<PackageStoreOptions> options,
    ILogger<FileSystemPackageStore> logger) : IPackageStore
{
    private string Root => options.Value.RootPath;

    public async Task<IReadOnlyList<PackageSummary>> ListAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(Root)) return [];

        var summaries = new List<PackageSummary>();
        foreach (var file in Directory.EnumerateFiles(Root, "*.nupkg", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var details = await ReadDetailsFromNupkgAsync(file, ct);
                if (details is not null)
                    summaries.Add(new PackageSummary
                    {
                        Id = details.Id,
                        Version = details.Version,
                        Description = details.Description,
                        Authors = details.Authors,
                        Published = details.Published
                    });
            }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to read {File}", file); }
        }

        return summaries;
    }

    public async Task<PackageDetails?> GetDetailsAsync(string packageId, string version, CancellationToken ct = default)
    {
        var path = FindNupkg(packageId, version);
        return path is null ? null : await ReadDetailsFromNupkgAsync(path, ct);
    }

    public Task<string?> GetNupkgPathAsync(string packageId, string version, CancellationToken ct = default)
        => Task.FromResult(FindNupkg(packageId, version));

    public async Task PushAsync(Stream nupkgStream, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Root);

        // Peek nuspec to get ID/version for file naming
        using var ms = new MemoryStream();
        await nupkgStream.CopyToAsync(ms, ct);
        ms.Position = 0;

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: true);
        var nuspecEntry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        if (nuspecEntry is null) throw new InvalidOperationException("No .nuspec found in package.");

        await using var nuspecStream = nuspecEntry.Open();
        var doc = await XDocument.LoadAsync(nuspecStream, LoadOptions.None, ct);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var meta = doc.Root?.Element(ns + "metadata");
        var id = meta?.Element(ns + "id")?.Value ?? throw new InvalidOperationException("Package ID missing.");
        var version = meta?.Element(ns + "version")?.Value ?? throw new InvalidOperationException("Version missing.");

        var dir = Path.Combine(Root, id.ToLowerInvariant(), version.ToLowerInvariant());
        Directory.CreateDirectory(dir);

        var dest = Path.Combine(dir, $"{id.ToLowerInvariant()}.{version.ToLowerInvariant()}.nupkg");
        ms.Position = 0;
        await using var fs = File.Create(dest);
        await ms.CopyToAsync(fs, ct);

        logger.LogInformation("Pushed {Id} v{Version} → {Path}", id, version, dest);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private string? FindNupkg(string packageId, string version)
    {
        var dir = Path.Combine(Root, packageId.ToLowerInvariant(), version.ToLowerInvariant());
        var path = Path.Combine(dir, $"{packageId.ToLowerInvariant()}.{version.ToLowerInvariant()}.nupkg");
        return File.Exists(path) ? path : null;
    }

    private async Task<PackageDetails?> ReadDetailsFromNupkgAsync(string path, CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
        var nuspecEntry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        if (nuspecEntry is null) return null;

        await using var ns2 = nuspecEntry.Open();
        var doc = await XDocument.LoadAsync(ns2, LoadOptions.None, ct);
        var xns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var meta = doc.Root?.Element(xns + "metadata");

        return new PackageDetails
        {
            Id = meta?.Element(xns + "id")?.Value ?? string.Empty,
            Version = meta?.Element(xns + "version")?.Value ?? string.Empty,
            Description = meta?.Element(xns + "description")?.Value,
            Authors = meta?.Element(xns + "authors")?.Value,
            ProjectUrl = meta?.Element(xns + "projectUrl")?.Value,
            LicenseExpression = meta?.Element(xns + "license")?.Value,
            Tags = meta?.Element(xns + "tags")?.Value,
            FileSizeBytes = new FileInfo(path).Length,
            NupkgPath = path
        };
    }
}

/// <summary>Configuration options for the file-system package store.</summary>
public sealed class PackageStoreOptions
{
    public const string SectionName = "PackageStore";

    /// <summary>Root directory where .nupkg files are stored.</summary>
    public string RootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "packages");
}
