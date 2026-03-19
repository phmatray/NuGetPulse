using NuGetPulse.Web.Services;
using TheAppManager.Modules;

namespace NuGetPulse.Web.Modules;

/// <summary>
/// Registers the NuGet API HttpClient with timeout and gzip decompression.
/// </summary>
public class NuGetApiModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<NuGetService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "NuGetPulse/1.0 (+https://nugetpulse.garry-ai.cloud)");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        });
    }
}
