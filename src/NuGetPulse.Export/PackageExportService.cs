using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using NuGetPulse.Core.Models;
using NuGetPulse.Export.Models;

namespace NuGetPulse.Export;

/// <summary>
/// CSV and JSON exporter for NuGet package references.
/// Ported from NugetManager's ExportService (CsvHelper-based),
/// modernised for .NET 10 and NuGetPulse's flat data model.
/// </summary>
public sealed class PackageExportService(ILogger<PackageExportService> logger) : IPackageExportService
{
    private static readonly JsonSerializerOptions IndentedJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions CompactJson = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    // ─── CSV ──────────────────────────────────────────────────────────────────

    public async Task<ExportResult> ExportToCsvAsync(
        IReadOnlyList<PackageReference> packages,
        string? title = null,
        CancellationToken ct = default)
    {
        logger.LogInformation("Exporting {Count} packages to CSV", packages.Count);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, config);

        var records = packages.Select(p => new PackageCsvRecord
        {
            PackageName = p.PackageName,
            Version = p.Version,
            ProjectFile = Path.GetFileName(p.ProjectFile),
            FullProjectPath = p.ProjectFile,
            Type = p.Type.ToString(),
            IsCentrallyManaged = p.IsCentrallyManaged ? "Yes" : "No",
            SourceType = p.SourceType.ToString(),
            VersionOverride = p.VersionOverride ?? string.Empty
        });

        csv.WriteRecords(records);
        await csv.FlushAsync();

        var data = writer.ToString();
        var bytes = Encoding.UTF8.GetBytes(data);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var safeName = string.IsNullOrWhiteSpace(title)
            ? "packages"
            : new string(title.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());

        logger.LogInformation("CSV export complete: {Bytes} bytes, {Count} records", bytes.Length, packages.Count);

        return new ExportResult
        {
            Format = ExportFormat.Csv,
            MimeType = "text/csv",
            FileName = $"{safeName}-{timestamp}.csv",
            Data = data,
            BinaryData = bytes
        };
    }

    // ─── JSON ─────────────────────────────────────────────────────────────────

    public async Task<ExportResult> ExportToJsonAsync(
        IReadOnlyList<PackageReference> packages,
        bool indented = true,
        CancellationToken ct = default)
    {
        logger.LogInformation("Exporting {Count} packages to JSON", packages.Count);

        var exportDoc = new
        {
            ExportedAt = DateTime.UtcNow,
            TotalPackages = packages.Count,
            Packages = packages.Select(p => new
            {
                p.PackageName,
                p.Version,
                ProjectFile = Path.GetFileName(p.ProjectFile),
                FullProjectPath = p.ProjectFile,
                Type = p.Type.ToString(),
                p.IsCentrallyManaged,
                VersionOverride = p.VersionOverride,
                SourceType = p.SourceType.ToString()
            })
        };

        var opts = indented ? IndentedJson : CompactJson;
        var data = JsonSerializer.Serialize(exportDoc, opts);
        var bytes = Encoding.UTF8.GetBytes(data);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        logger.LogInformation("JSON export complete: {Bytes} bytes", bytes.Length);

        return await Task.FromResult(new ExportResult
        {
            Format = ExportFormat.Json,
            MimeType = "application/json",
            FileName = $"packages-{timestamp}.json",
            Data = data,
            BinaryData = bytes
        });
    }

    // ─── CSV record ───────────────────────────────────────────────────────────

    private sealed class PackageCsvRecord
    {
        public string PackageName { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public string ProjectFile { get; init; } = string.Empty;
        public string FullProjectPath { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string IsCentrallyManaged { get; init; } = string.Empty;
        public string SourceType { get; init; } = string.Empty;
        public string VersionOverride { get; init; } = string.Empty;
    }
}
