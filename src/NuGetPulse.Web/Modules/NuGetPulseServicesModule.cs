using NuGetPulse.Export;
using NuGetPulse.Graph;
using NuGetPulse.Scanner;
using NuGetPulse.Security;
using TheAppManager.Modules;

namespace NuGetPulse.Web.Modules;

/// <summary>
/// Registers NuGetPulse domain services: Scanner, Security, Graph, and Export.
/// </summary>
public class NuGetPulseServicesModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddNuGetPulseScanner();
        builder.Services.AddNuGetPulseSecurity();
        builder.Services.AddNuGetPulseGraph();
        builder.Services.AddNuGetPulseExport();
    }
}
