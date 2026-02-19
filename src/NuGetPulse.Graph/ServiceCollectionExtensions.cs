using Microsoft.Extensions.DependencyInjection;

namespace NuGetPulse.Graph;

/// <summary>DI registration for the NuGetPulse dependency graph services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Register the dependency graph builder.</summary>
    public static IServiceCollection AddNuGetPulseGraph(this IServiceCollection services)
    {
        services.AddSingleton<IDependencyGraphBuilder, DependencyGraphBuilder>();
        return services;
    }
}
