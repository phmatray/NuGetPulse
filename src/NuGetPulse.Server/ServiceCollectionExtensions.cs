using Microsoft.Extensions.DependencyInjection;
using NuGetPulse.Server.Abstractions;

namespace NuGetPulse.Server;

/// <summary>Extension methods to register NuGetPulse.Server services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the self-hosted NuGet package server services to the DI container.
    /// </summary>
    public static IServiceCollection AddNuGetPulseServer(
        this IServiceCollection services,
        Action<PackageStoreOptions>? configure = null)
    {
        var opts = new PackageStoreOptions();
        configure?.Invoke(opts);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(opts));
        services.AddSingleton<IPackageStore, FileSystemPackageStore>();
        return services;
    }
}
