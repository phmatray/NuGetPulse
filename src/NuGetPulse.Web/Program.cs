using NuGetPulse.Web.Modules;
using TheAppManager.Startup;

AppManager.Start(args, modules =>
{
    modules
        .Add<NuGetApiModule>()
        .Add<NuGetPulseServicesModule>()
        .Add<PersistenceModule>()
        .Add<InfrastructureModule>()
        .Add<BlazorModule>();
});
