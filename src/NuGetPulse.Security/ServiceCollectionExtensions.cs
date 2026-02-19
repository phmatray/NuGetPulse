using Microsoft.Extensions.DependencyInjection;
using NuGetPulse.Core.Abstractions;

namespace NuGetPulse.Security;

/// <summary>Extension methods to register NuGetPulse.Security services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the OSV vulnerability scanner to the DI container.
    /// </summary>
    public static IServiceCollection AddNuGetPulseSecurity(this IServiceCollection services)
    {
        services.AddHttpClient<IVulnerabilityScanner, OsvClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.osv.dev/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
