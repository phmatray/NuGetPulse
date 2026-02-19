using NuGetPulse.Core.Models;
using NuGetPulse.Persistence.Entities;

namespace NuGetPulse.Persistence;

/// <summary>Stores and retrieves scan sessions and their package data.</summary>
public interface IScanHistoryRepository
{
    /// <summary>Persist a new scan session with all discovered packages.</summary>
    Task<ScanSession> SaveScanAsync(
        string name,
        string path,
        IReadOnlyList<PackageReference> packages,
        long durationMs,
        int vulnerabilityCount = 0,
        CancellationToken ct = default);

    /// <summary>Get recent scan sessions (most recent first).</summary>
    Task<IReadOnlyList<ScanSession>> GetRecentSessionsAsync(int limit = 20, CancellationToken ct = default);

    /// <summary>Get a single scan session by ID, including its packages.</summary>
    Task<ScanSession?> GetSessionAsync(int id, CancellationToken ct = default);

    /// <summary>Get all packages seen across all scans for a given package name.</summary>
    Task<IReadOnlyList<ScannedPackage>> GetPackageHistoryAsync(string packageName, CancellationToken ct = default);

    /// <summary>Delete sessions older than the cutoff date.</summary>
    Task<int> PurgeOldSessionsAsync(DateTime olderThan, CancellationToken ct = default);
}
