using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NuGetPulse.Core.Abstractions;
using NuGetPulse.Core.Models;

namespace NuGetPulse.Scanner;

/// <summary>
/// Scans project files (.csproj, .fsproj, packages.config, Directory.Packages.props)
/// to extract NuGet package references.
///
/// Ported and modernised from NugetManager.PackageScannerService.
/// </summary>
public sealed class PackageScanner(ILogger<PackageScanner> logger) : IPackageScanner
{
    private static readonly string[] SupportedExtensions = [".csproj", ".fsproj"];
    private const string PackagesConfigName = "packages.config";
    private const string DirPackagesPropsName = "Directory.Packages.props";

    // ─── Public API ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<PackageReference>> ScanProjectFileAsync(
        string filePath, CancellationToken ct = default)
    {
        ValidateFile(filePath);
        if (!IsSupportedProjectFile(filePath))
            throw new ArgumentException($"Unsupported file type: {filePath}");

        var doc = await ParseXmlAsync(filePath, ct);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var packages = new List<PackageReference>();

        foreach (var el in doc.Descendants(ns + "PackageReference"))
        {
            var name = el.Attribute("Include")?.Value;
            var version = el.Attribute("Version")?.Value ?? el.Element(ns + "Version")?.Value ?? "CPM";
            if (!string.IsNullOrWhiteSpace(name))
                packages.Add(new PackageReference
                {
                    PackageName = name.Trim(),
                    Version = version.Trim(),
                    ProjectFile = filePath,
                    Type = PackageType.PackageReference,
                    SourceFile = filePath,
                    SourceType = PackageSourceType.ProjectFile
                });
        }

        logger.LogDebug("Extracted {Count} packages from {File}", packages.Count, filePath);
        return packages;
    }

    public async Task<IReadOnlyList<PackageReference>> ScanPackagesConfigAsync(
        string filePath, CancellationToken ct = default)
    {
        ValidateFile(filePath);
        var doc = await ParseXmlAsync(filePath, ct);
        var packages = new List<PackageReference>();

        foreach (var el in doc.Descendants("package"))
        {
            var name = el.Attribute("id")?.Value;
            var version = el.Attribute("version")?.Value;
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(version))
                packages.Add(new PackageReference
                {
                    PackageName = name.Trim(),
                    Version = version.Trim(),
                    ProjectFile = filePath,
                    Type = PackageType.PackagesConfig,
                    SourceFile = filePath,
                    SourceType = PackageSourceType.PackagesConfig
                });
        }

        logger.LogDebug("Extracted {Count} packages from {File}", packages.Count, filePath);
        return packages;
    }

    public async Task<IReadOnlyList<PackageReference>> ScanDirectoryPackagesPropsAsync(
        string filePath, CancellationToken ct = default)
    {
        ValidateFile(filePath);
        var doc = await ParseXmlAsync(filePath, ct);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var packages = new List<PackageReference>();

        foreach (var el in doc.Descendants(ns + "PackageVersion"))
        {
            var name = el.Attribute("Include")?.Value;
            var version = el.Attribute("Version")?.Value;
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(version))
                packages.Add(new PackageReference
                {
                    PackageName = name.Trim(),
                    Version = version.Trim(),
                    ProjectFile = filePath,
                    Type = PackageType.PackageReference,
                    IsCentrallyManaged = true,
                    SourceFile = filePath,
                    SourceType = PackageSourceType.DirectoryPackagesProps
                });
        }

        logger.LogDebug("Extracted {Count} central packages from {File}", packages.Count, filePath);
        return packages;
    }

    public async Task<IReadOnlyList<PackageReference>> ScanDirectoryAsync(
        string directoryPath, CancellationToken ct = default)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var results = new List<PackageReference>();

        // Central package management file
        var dpp = Path.Combine(directoryPath, DirPackagesPropsName);
        if (File.Exists(dpp))
            results.AddRange(await ScanDirectoryPackagesPropsAsync(dpp, ct));

        // All project files
        foreach (var ext in SupportedExtensions)
        {
            foreach (var file in Directory.EnumerateFiles(directoryPath, $"*{ext}", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                try { results.AddRange(await ScanProjectFileAsync(file, ct)); }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to scan {File}", file); }
            }
        }

        // Legacy packages.config files
        foreach (var file in Directory.EnumerateFiles(directoryPath, PackagesConfigName, SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            try { results.AddRange(await ScanPackagesConfigAsync(file, ct)); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to scan {File}", file); }
        }

        // Deduplicate by (Name, Version, SourceFile)
        return results
            .DistinctBy(p => (p.PackageName, p.Version, p.SourceFile))
            .ToList();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsSupportedProjectFile(string filePath)
        => SupportedExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase)
           || Path.GetFileName(filePath).Equals(PackagesConfigName, StringComparison.OrdinalIgnoreCase);

    private static void ValidateFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
    }

    private async Task<XDocument> ParseXmlAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath, ct);
            return XDocument.Parse(content);
        }
        catch (XmlException ex)
        {
            logger.LogError(ex, "XML parse error in {File}", filePath);
            throw;
        }
    }
}
