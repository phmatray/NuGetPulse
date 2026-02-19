using NuGetPulse.Core.Models;
using NuGetPulse.Export.Models;

namespace NuGetPulse.Export;

/// <summary>
/// Exports a list of scanned package references to CSV or JSON.
/// Ported from NugetManager's ExportService, simplified for NuGetPulse's flat package model.
/// </summary>
public interface IPackageExportService
{
    /// <summary>Export packages to CSV format (compatible with Excel).</summary>
    Task<ExportResult> ExportToCsvAsync(
        IReadOnlyList<PackageReference> packages,
        string? title = null,
        CancellationToken ct = default);

    /// <summary>Export packages to JSON format.</summary>
    Task<ExportResult> ExportToJsonAsync(
        IReadOnlyList<PackageReference> packages,
        bool indented = true,
        CancellationToken ct = default);
}
