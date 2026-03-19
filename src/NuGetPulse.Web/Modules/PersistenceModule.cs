using NuGetPulse.Persistence;
using TheAppManager.Modules;

namespace NuGetPulse.Web.Modules;

/// <summary>
/// Configures EF Core SQLite persistence and ensures the database is created at startup.
/// </summary>
public class PersistenceModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var dbPath = builder.Configuration.GetValue<string>("Database:Path")
                     ?? Path.Combine(builder.Environment.ContentRootPath, "nugetpulse.db");
        builder.Services.AddNuGetPulsePersistence(dbPath);
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        // Ensure database exists (creates tables if not present)
        app.Services.MigrateNuGetPulseDbAsync().GetAwaiter().GetResult();
    }
}
