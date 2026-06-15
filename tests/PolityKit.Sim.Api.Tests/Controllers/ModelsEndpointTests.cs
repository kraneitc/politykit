using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PolityKit.Sim.Api.Contracts;

namespace PolityKit.Sim.Api.Tests.Controllers;

public sealed class ModelsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
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

    [Fact]
    public async Task GetModelsReturnsGovernancePresetMetadata()
    {
        var client = factory.CreateClient();

        var models = await client.GetFromJsonAsync<ModelResponse[]>("/api/models");

        Assert.NotNull(models);
        var model = Assert.Single(models, candidate => candidate.Name == "CompositeGovernance:regulated-market");
        Assert.Equal("governance-preset", model.Kind);
        Assert.NotNull(model.Preset);
        Assert.Equal("regulated-market", model.Preset.Id);
        Assert.Equal("Regulated Market", model.Preset.Name);
        Assert.NotEmpty(model.Preset.Assumptions);
        Assert.NotEmpty(model.Preset.KnownFailureModes);
    }
}
