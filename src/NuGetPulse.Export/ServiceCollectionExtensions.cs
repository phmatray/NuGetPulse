using Microsoft.Extensions.DependencyInjection;

namespace NuGetPulse.Export;

/// <summary>DI registration for NuGetPulse export services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Register the CSV/JSON package export service.</summary>
    public static IServiceCollection AddNuGetPulseExport(this IServiceCollection services)
    {
        services.AddScoped<IPackageExportService, PackageExportService>();
        return services;
    }
}
