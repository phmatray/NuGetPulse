using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace NuGetPulse.Persistence;

/// <summary>DI registration for the NuGetPulse persistence layer.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the EF Core SQLite persistence layer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databasePath">
    /// Path to the SQLite database file.
    /// Defaults to <c>nugetpulse.db</c> in the application base directory.
    /// </param>
    public static IServiceCollection AddNuGetPulsePersistence(
        this IServiceCollection services,
        string? databasePath = null)
    {
        databasePath ??= Path.Combine(AppContext.BaseDirectory, "nugetpulse.db");

        services.AddDbContext<NuGetPulseDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        services.AddScoped<IScanHistoryRepository, ScanHistoryRepository>();

        return services;
    }

    /// <summary>
    /// Ensure the database is created and all pending migrations applied.
    /// Call at startup (e.g. <c>app.Services.MigrateNuGetPulseDbAsync()</c>).
    /// </summary>
    public static async Task MigrateNuGetPulseDbAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NuGetPulseDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
