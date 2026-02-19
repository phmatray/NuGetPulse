using Microsoft.EntityFrameworkCore;
using NuGetPulse.Persistence.Entities;

namespace NuGetPulse.Persistence;

/// <summary>
/// EF Core SQLite persistence context for NuGetPulse.
/// Provides scan history, package tracking over time.
///
/// Ported from NugetManager's ApplicationDbContext, modernised for .NET 10 / EF Core 10.
/// </summary>
public sealed class NuGetPulseDbContext : DbContext
{
    public NuGetPulseDbContext(DbContextOptions<NuGetPulseDbContext> options) : base(options) { }

    public DbSet<ScanSession> ScanSessions { get; set; }
    public DbSet<ScannedPackage> ScannedPackages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ScanSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ScannedAt).IsRequired();
            entity.HasIndex(e => e.ScannedAt);
            entity.HasMany(e => e.Packages)
                  .WithOne(p => p.ScanSession)
                  .HasForeignKey(p => p.ScanSessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScannedPackage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PackageName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProjectFile).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => e.PackageName);
            entity.HasIndex(e => new { e.ScanSessionId, e.PackageName });
        });
    }
}
