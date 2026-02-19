using Microsoft.EntityFrameworkCore;
using NuGetPulse.Core.Models;
using NuGetPulse.Persistence.Entities;

namespace NuGetPulse.Persistence;

/// <summary>EF Core-backed implementation of <see cref="IScanHistoryRepository"/>.</summary>
public sealed class ScanHistoryRepository(NuGetPulseDbContext db) : IScanHistoryRepository
{
    public async Task<ScanSession> SaveScanAsync(
        string name,
        string path,
        IReadOnlyList<PackageReference> packages,
        long durationMs,
        int vulnerabilityCount = 0,
        CancellationToken ct = default)
    {
        var session = new ScanSession
        {
            Name = name,
            Path = path,
            ScannedAt = DateTime.UtcNow,
            DurationMs = durationMs,
            PackageCount = packages.Count,
            VulnerabilityCount = vulnerabilityCount,
            Status = ScanSessionStatus.Completed,
            Packages = packages.Select(p => new ScannedPackage
            {
                PackageName = p.PackageName,
                Version = p.Version,
                ProjectFile = p.ProjectFile,
                Type = p.Type,
                IsCentrallyManaged = p.IsCentrallyManaged,
                VersionOverride = p.VersionOverride,
                SourceFile = p.SourceFile,
                SourceType = p.SourceType
            }).ToList()
        };

        db.ScanSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<IReadOnlyList<ScanSession>> GetRecentSessionsAsync(int limit = 20, CancellationToken ct = default)
        => await db.ScanSessions
            .OrderByDescending(s => s.ScannedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<ScanSession?> GetSessionAsync(int id, CancellationToken ct = default)
        => await db.ScanSessions
            .Include(s => s.Packages)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<ScannedPackage>> GetPackageHistoryAsync(
        string packageName, CancellationToken ct = default)
        => await db.ScannedPackages
            .Where(p => p.PackageName == packageName)
            .OrderByDescending(p => p.ScanSession.ScannedAt)
            .Include(p => p.ScanSession)
            .ToListAsync(ct);

    public async Task<int> PurgeOldSessionsAsync(DateTime olderThan, CancellationToken ct = default)
    {
        var old = await db.ScanSessions
            .Where(s => s.ScannedAt < olderThan)
            .ToListAsync(ct);

        db.ScanSessions.RemoveRange(old);
        await db.SaveChangesAsync(ct);
        return old.Count;
    }
}
