using Microsoft.Extensions.Logging.Abstractions;
using NuGetPulse.Core.Models;
using NuGetPulse.Scanner;
using FluentAssertions;

namespace NuGetPulse.Tests.Scanner;

/// <summary>
/// Unit tests for <see cref="PackageScanner"/>.
/// Creates temporary project files to validate parsing logic.
/// </summary>
public sealed class PackageScannerTests : IDisposable
{
    private readonly PackageScanner _sut = new(NullLogger<PackageScanner>.Instance);
    private readonly List<string> _tempFiles = [];

    // ─── ScanProjectFileAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ScanProjectFile_SdkCsproj_ExtractsPackageReferences()
    {
        var path = WriteTempFile("test.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                <PackageReference Include="FluentAssertions" Version="7.0.0" />
              </ItemGroup>
            </Project>
            """);

        var results = await _sut.ScanProjectFileAsync(path);

        results.Should().HaveCount(2);
        results.Should().Contain(p => p.PackageName == "Newtonsoft.Json" && p.Version == "13.0.3");
        results.Should().Contain(p => p.PackageName == "FluentAssertions" && p.Version == "7.0.0");
        results.Should().AllSatisfy(p => p.SourceType.Should().Be(PackageSourceType.ProjectFile));
    }

    [Fact]
    public async Task ScanProjectFile_CpmProject_VersionMarkedAsCpm()
    {
        // In CPM projects, the <PackageReference> has no Version attribute
        var path = WriteTempFile("test.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="NSubstitute" />
              </ItemGroup>
            </Project>
            """);

        var results = await _sut.ScanProjectFileAsync(path);

        results.Should().HaveCount(1);
        results[0].PackageName.Should().Be("NSubstitute");
        results[0].Version.Should().Be("CPM");
    }

    [Fact]
    public async Task ScanProjectFile_EmptyProject_ReturnsEmpty()
    {
        var path = WriteTempFile("empty.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var results = await _sut.ScanProjectFileAsync(path);

        results.Should().BeEmpty();
    }

    // ─── ScanPackagesConfigAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ScanPackagesConfig_ValidFile_ExtractsAllPackages()
    {
        var path = WriteTempFile("packages.config", """
            <?xml version="1.0" encoding="utf-8"?>
            <packages>
              <package id="log4net" version="2.0.15" targetFramework="net48" />
              <package id="Newtonsoft.Json" version="13.0.1" />
            </packages>
            """);

        var results = await _sut.ScanPackagesConfigAsync(path);

        results.Should().HaveCount(2);
        results.Should().Contain(p => p.PackageName == "log4net" && p.Version == "2.0.15");
        results.Should().Contain(p => p.PackageName == "Newtonsoft.Json");
        results.Should().AllSatisfy(p => p.Type.Should().Be(PackageType.PackagesConfig));
    }

    // ─── ScanDirectoryPackagesPropsAsync ──────────────────────────────────────

    [Fact]
    public async Task ScanDirectoryPackagesProps_ValidFile_ExtractsCentralVersions()
    {
        var path = WriteTempFile("Directory.Packages.props", """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Microsoft.Extensions.Logging" Version="10.0.0" />
                <PackageVersion Include="CsvHelper" Version="33.1.0" />
              </ItemGroup>
            </Project>
            """);

        var results = await _sut.ScanDirectoryPackagesPropsAsync(path);

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(p => p.IsCentrallyManaged.Should().BeTrue());
        results.Should().AllSatisfy(p =>
            p.SourceType.Should().Be(PackageSourceType.DirectoryPackagesProps));
        results.Should().Contain(p => p.PackageName == "Microsoft.Extensions.Logging" && p.Version == "10.0.0");
    }

    // ─── ScanDirectoryAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ScanDirectory_MultipleProjectFiles_DeduplicatesResults()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"nugetpulse-scan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        var proj1 = Path.Combine(dir, "App.csproj");
        var proj2 = Path.Combine(dir, "Lib.csproj");

        File.WriteAllText(proj1, """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Serilog" Version="4.0.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(proj2, """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Serilog" Version="4.0.0" />
                <PackageReference Include="FluentAssertions" Version="7.0.0" />
              </ItemGroup>
            </Project>
            """);

        try
        {
            var results = await _sut.ScanDirectoryAsync(dir);

            // Deduplication is by (Name, Version, SourceFile):
            // Serilog 4.0.0 in App.csproj, Serilog 4.0.0 in Lib.csproj = 2 entries (different source files)
            // FluentAssertions 7.0.0 in Lib.csproj = 1 entry
            // Total = 3
            results.Should().HaveCount(3,
                "deduplication is per (Name, Version, SourceFile): Serilog appears in 2 files + FluentAssertions in 1 file");
            results.Where(p => p.PackageName == "Serilog").Should().HaveCount(2);
            results.Where(p => p.PackageName == "FluentAssertions").Should().HaveCount(1);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // ─── Error handling ───────────────────────────────────────────────────────

    [Fact]
    public async Task ScanProjectFile_FileNotFound_ThrowsFileNotFoundException()
    {
        var act = async () => await _sut.ScanProjectFileAsync("/non-existent/file.csproj");
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ScanProjectFile_UnsupportedExtension_ThrowsArgumentException()
    {
        var path = WriteTempFile("readme.txt", "hello");
        var act = async () => await _sut.ScanProjectFileAsync(path);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private string WriteTempFile(string name, string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"npp-test-{Guid.NewGuid():N}-{name}");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var f in _tempFiles)
            if (File.Exists(f)) File.Delete(f);
    }
}
