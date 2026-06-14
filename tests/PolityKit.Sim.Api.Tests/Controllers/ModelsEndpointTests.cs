using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PolityKit.Sim.Api;
using PolityKit.Sim.Api.Contracts;

namespace PolityKit.Sim.Api.Tests.Controllers;

public sealed class ModelsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ModelsEndpointTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetModelsReturnsDefaultModels()
    {
        var client = factory.CreateClient();

        var models = await client.GetFromJsonAsync<ModelResponse[]>("/api/models");

        Assert.NotNull(models);
        Assert.Contains(models, model => model.Name == "NeedBasedAllocation");
        Assert.Contains(models, model => model.Name == "MarketBasedAllocation");
        Assert.Contains(models, model => model.Name == "HierarchyBasedAllocation");
    }
}
