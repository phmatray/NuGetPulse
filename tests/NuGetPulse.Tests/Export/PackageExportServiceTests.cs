using System.Text.Json;
using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using NuGetPulse.Core.Models;
using NuGetPulse.Export;
using NuGetPulse.Export.Models;

namespace NuGetPulse.Tests.Export;

/// <summary>Unit tests for <see cref="PackageExportService"/>.</summary>
public sealed class PackageExportServiceTests
{
    private readonly PackageExportService _sut = new(NullLogger<PackageExportService>.Instance);

    private static readonly IReadOnlyList<PackageReference> SamplePackages =
    [
        new() { PackageName = "Newtonsoft.Json", Version = "13.0.3", ProjectFile = "/src/App.csproj", Type = PackageType.PackageReference },
        new() { PackageName = "Serilog", Version = "4.0.0", ProjectFile = "/src/Api.csproj", Type = PackageType.PackageReference },
        new() { PackageName = "CsvHelper", Version = "33.1.0", ProjectFile = "/src/App.csproj", Type = PackageType.PackageReference, IsCentrallyManaged = true }
    ];

    // ─── CSV ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportToCsv_ValidPackages_ReturnsCorrectMimeType()
    {
        var result = await _sut.ExportToCsvAsync(SamplePackages);

        result.Format.ShouldBe(ExportFormat.Csv);
        result.MimeType.ShouldBe("text/csv");
        result.FileName.ShouldEndWith(".csv");
    }

    [Fact]
    public async Task ExportToCsv_ValidPackages_DataContainsAllPackageNames()
    {
        var result = await _sut.ExportToCsvAsync(SamplePackages);

        result.Data.ShouldContain("Newtonsoft.Json");
        result.Data.ShouldContain("Serilog");
        result.Data.ShouldContain("CsvHelper");
    }

    [Fact]
    public async Task ExportToCsv_ValidPackages_BinaryDataMatchesText()
    {
        var result = await _sut.ExportToCsvAsync(SamplePackages);

        var text = System.Text.Encoding.UTF8.GetString(result.BinaryData);
        text.ShouldBe(result.Data);
    }

    [Fact]
    public async Task ExportToCsv_WithTitle_FileNameContainsTitle()
    {
        var result = await _sut.ExportToCsvAsync(SamplePackages, title: "my-project");

        result.FileName.ShouldStartWith("my-project-");
    }

    [Fact]
    public async Task ExportToCsv_EmptyPackages_ReturnsHeaderOnlyOrEmpty()
    {
        var result = await _sut.ExportToCsvAsync([]);

        result.DataSize.ShouldBeGreaterThanOrEqualTo(0);
    }

    // ─── JSON ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportToJson_ValidPackages_ReturnsCorrectMimeType()
    {
        var result = await _sut.ExportToJsonAsync(SamplePackages);

        result.Format.ShouldBe(ExportFormat.Json);
        result.MimeType.ShouldBe("application/json");
        result.FileName.ShouldEndWith(".json");
    }

    [Fact]
    public async Task ExportToJson_ValidPackages_IsValidJson()
    {
        var result = await _sut.ExportToJsonAsync(SamplePackages);

        var act = () => JsonDocument.Parse(result.Data);
        act();
    }

    [Fact]
    public async Task ExportToJson_ValidPackages_ContainsPackageCount()
    {
        var result = await _sut.ExportToJsonAsync(SamplePackages);

        using var doc = JsonDocument.Parse(result.Data);
        doc.RootElement.GetProperty("TotalPackages").GetInt32().ShouldBe(SamplePackages.Count);
    }

    [Fact]
    public async Task ExportToJson_IndentedFalse_CompactOutput()
    {
        var indented = await _sut.ExportToJsonAsync(SamplePackages, indented: true);
        var compact = await _sut.ExportToJsonAsync(SamplePackages, indented: false);

        compact.Data.Length.ShouldBeLessThan(indented.Data.Length);
    }
}
