using Microsoft.AspNetCore.DataProtection;
using TheAppManager.Modules;

namespace NuGetPulse.Web.Modules;

/// <summary>
/// Configures infrastructure concerns: Data Protection, Health Checks,
/// error handling, HTTPS redirection, and status code pages.
/// </summary>
public class InfrastructureModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        // Data Protection (persist keys for multi-pod deployments)
        var keysPath = builder.Configuration.GetValue<string>("DataProtection:KeysPath")
                       ?? Path.Combine(builder.Environment.ContentRootPath, "keys");
        Directory.CreateDirectory(keysPath);
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("NuGetPulse");

        // Health Checks
        builder.Services.AddHealthChecks();
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
    }
}
