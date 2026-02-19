using Microsoft.Extensions.DependencyInjection;
using NuGetPulse.Core.Abstractions;

namespace NuGetPulse.Scanner;

/// <summary>Extension methods to register NuGetPulse.Scanner services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the project-file package scanner to the DI container.
    /// </summary>
    public static IServiceCollection AddNuGetPulseScanner(this IServiceCollection services)
    {
        services.AddSingleton<IPackageScanner, PackageScanner>();
        return services;
    }
}
