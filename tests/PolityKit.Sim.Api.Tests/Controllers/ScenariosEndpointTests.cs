using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PolityKit.Sim.Api.Contracts;

namespace PolityKit.Sim.Api.Tests.Controllers;

public sealed class ScenariosEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetScenariosReturnsBuiltInScenarioMetadata()
    {
        var client = factory.CreateClient();

        var scenarios = await client.GetFromJsonAsync<ScenarioResponse[]>("/api/scenarios");

        Assert.NotNull(scenarios);

        var scenario = Assert.Single(scenarios);
        Assert.Equal("Village Food Crisis", scenario.Name);
        Assert.Equal("village-food-crisis", scenario.Slug);
        Assert.Equal(12345, scenario.Seed);
        Assert.Equal(120, scenario.Ticks);
        Assert.Equal(500, scenario.InitialPopulation);
        Assert.Collection(
            scenario.Shocks,
            shock =>
            {
                Assert.Equal(20, shock.Tick);
                Assert.Equal("CropFailure", shock.Type);
                Assert.Equal(0.4, shock.Severity);
            },
            shock =>
            {
                Assert.Equal(45, shock.Tick);
                Assert.Equal("AdministrativeOverload", shock.Type);
                Assert.Equal(0.3, shock.Severity);
            });
    }
}
