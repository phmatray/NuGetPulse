using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NuGetPulse.Core.Models;
using NuGetPulse.Persistence;
using NuGetPulse.Persistence.Entities;

namespace NuGetPulse.Tests.Persistence;

/// <summary>
/// Integration tests for <see cref="ScanHistoryRepository"/> using an in-process
/// SQLite database. A shared, kept-open connection ensures the in-memory DB
/// persists for the lifetime of the test instance.
/// </summary>
public sealed class ScanHistoryRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly NuGetPulseDbContext _db;
    private readonly ScanHistoryRepository _sut;

    public ScanHistoryRepositoryTests()
    {
        // Use a shared connection so the in-memory SQLite DB is not lost between operations
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<NuGetPulseDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new NuGetPulseDbContext(options);
        _db.Database.EnsureCreated();

        _sut = new ScanHistoryRepository(_db);
    }

    private static IReadOnlyList<PackageReference> SamplePackages(int count = 3) =>
        Enumerable.Range(1, count).Select(i => new PackageReference
        {
            PackageName = $"Package{i}",
            Version = $"1.0.{i}",
            ProjectFile = $"/src/App{i}.csproj",
            Type = PackageType.PackageReference,
            IsCentrallyManaged = i % 2 == 0
        }).ToList();

    // ─── SaveScanAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveScan_ValidInput_PersistsSessionWithPackages()
    {
        var packages = SamplePackages(3);
        var session = await _sut.SaveScanAsync("MyProject", "/src/MyProject", packages, durationMs: 42);

        session.Id.Should().BeGreaterThan(0);
        session.Name.Should().Be("MyProject");
        session.Path.Should().Be("/src/MyProject");
        session.PackageCount.Should().Be(3);
        session.DurationMs.Should().Be(42);
        session.Status.Should().Be(ScanSessionStatus.Completed);
        session.Packages.Should().HaveCount(3);
    }

    [Fact]
    public async Task SaveScan_WithVulnerabilityCount_PersistsVulnCount()
    {
        var session = await _sut.SaveScanAsync("Proj", "/src", SamplePackages(1), 100, vulnerabilityCount: 5);

        session.VulnerabilityCount.Should().Be(5);
    }

    [Fact]
    public async Task SaveScan_EmptyPackages_PersistsEmptySession()
    {
        var session = await _sut.SaveScanAsync("Empty", "/src/empty", [], durationMs: 10);

        session.PackageCount.Should().Be(0);
        session.Packages.Should().BeEmpty();
    }

    // ─── GetRecentSessionsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetRecentSessions_ReturnsMostRecentFirst()
    {
        await _sut.SaveScanAsync("First", "/a", SamplePackages(1), 10);
        await Task.Delay(5); // ensure distinct timestamps
        await _sut.SaveScanAsync("Second", "/b", SamplePackages(2), 20);
        await Task.Delay(5);
        await _sut.SaveScanAsync("Third", "/c", SamplePackages(3), 30);

        var sessions = await _sut.GetRecentSessionsAsync(10);

        sessions.Should().HaveCount(3);
        sessions[0].Name.Should().Be("Third", "most recent first");
        sessions[2].Name.Should().Be("First", "oldest last");
    }

    [Fact]
    public async Task GetRecentSessions_LimitRespected()
    {
        for (int i = 0; i < 5; i++)
            await _sut.SaveScanAsync($"Scan{i}", $"/path{i}", SamplePackages(1), 10);

        var sessions = await _sut.GetRecentSessionsAsync(limit: 3);

        sessions.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetRecentSessions_NoScans_ReturnsEmpty()
    {
        var sessions = await _sut.GetRecentSessionsAsync();

        sessions.Should().BeEmpty();
    }

    // ─── GetSessionAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetSession_ExistingId_ReturnsSessionWithPackages()
    {
        var saved = await _sut.SaveScanAsync("Proj", "/src", SamplePackages(2), 50);

        var retrieved = await _sut.GetSessionAsync(saved.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Proj");
        retrieved.Packages.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSession_NonExistentId_ReturnsNull()
    {
        var result = await _sut.GetSessionAsync(999);

        result.Should().BeNull();
    }

    // ─── GetPackageHistoryAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetPackageHistory_PackageInMultipleScans_ReturnsAll()
    {
        var pkgs1 = new List<PackageReference>
        {
            new() { PackageName = "Serilog", Version = "3.0.0", ProjectFile = "/a.csproj", Type = PackageType.PackageReference }
        };
        var pkgs2 = new List<PackageReference>
        {
            new() { PackageName = "Serilog", Version = "4.0.0", ProjectFile = "/b.csproj", Type = PackageType.PackageReference }
        };

        await _sut.SaveScanAsync("S1", "/a", pkgs1, 10);
        await _sut.SaveScanAsync("S2", "/b", pkgs2, 20);

        var history = await _sut.GetPackageHistoryAsync("Serilog");

        history.Should().HaveCount(2);
        history.Should().Contain(p => p.Version == "3.0.0");
        history.Should().Contain(p => p.Version == "4.0.0");
    }

    [Fact]
    public async Task GetPackageHistory_UnknownPackage_ReturnsEmpty()
    {
        var history = await _sut.GetPackageHistoryAsync("NonExistentPackage");

        history.Should().BeEmpty();
    }

    // ─── PurgeOldSessionsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task PurgeOldSessions_DeletesSessionsOlderThanCutoff()
    {
        await _sut.SaveScanAsync("Old1", "/old1", SamplePackages(1), 10);
        await _sut.SaveScanAsync("Old2", "/old2", SamplePackages(1), 10);

        // Manually backdate the scanned_at timestamps
        var sessions = await _db.ScanSessions.ToListAsync();
        foreach (var s in sessions)
            s.ScannedAt = DateTime.UtcNow.AddDays(-40);
        await _db.SaveChangesAsync();

        // Save a recent scan
        await _sut.SaveScanAsync("Recent", "/recent", SamplePackages(2), 30);

        var cutoff = DateTime.UtcNow.AddDays(-30);
        var deleted = await _sut.PurgeOldSessionsAsync(cutoff);

        deleted.Should().Be(2);

        var remaining = await _sut.GetRecentSessionsAsync(100);
        remaining.Should().HaveCount(1);
        remaining[0].Name.Should().Be("Recent");
    }

    // ─── Cleanup ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
