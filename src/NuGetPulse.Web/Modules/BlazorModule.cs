using NuGetPulse.Web.Components;
using TheAppManager.Modules;

namespace NuGetPulse.Web.Modules;

/// <summary>
/// Configures Blazor Server-Side Rendering with interactive server components.
/// </summary>
public class BlazorModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        app.UseAntiforgery();
        app.MapStaticAssets();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }
}
