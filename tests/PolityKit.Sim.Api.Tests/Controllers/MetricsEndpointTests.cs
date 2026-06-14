using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PolityKit.Sim.Api.Tests.Controllers;

public sealed class MetricsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetMetricsReturnsDefaultMetricNames()
    {
        var client = factory.CreateClient();

        var metrics = await client.GetFromJsonAsync<MetricListItem[]>("/api/metrics");

        Assert.NotNull(metrics);

        var names = metrics.Select(metric => metric.Name).ToArray();
        Assert.Contains("Needs Met", names);
        Assert.Contains("Inequality", names);
        Assert.Contains("Trust", names);
        Assert.Contains("Severe Failures", names);
        Assert.Contains("Administrative Load", names);
    }

    private sealed class MetricListItem
    {
        public string Name { get; init; } = "";
    }
}
