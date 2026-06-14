using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
}
