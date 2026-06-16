using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PolityKit.Sim.Analysis;
using PolityKit.Sim.Api.Services;

namespace PolityKit.Sim.Api.Tests.TestHost;

public static class PolityKitApiFactory
{
    public static WebApplicationFactory<Program> WithIsolatedRunStore(this WebApplicationFactory<Program> factory)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRunStore>();
                services.AddSingleton<IRunStore, InMemoryRunStore>();
            });
        });
    }

    public static WebApplicationFactory<Program> WithAiProvider(
        this WebApplicationFactory<Program> factory,
        IAiAnalysisProvider provider)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAiAnalysisProvider>();
                services.RemoveAll<AiAnalysisService>();
                services.AddSingleton(provider);
                services.AddSingleton(new AiAnalysisService(provider, new AiAnalysisOptions
                {
                    Enabled = true,
                    ProviderName = provider.ProviderName
                }));
            });
        });
    }
}
