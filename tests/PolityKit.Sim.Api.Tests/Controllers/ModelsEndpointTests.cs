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
        Assert.Equal(6, model.GovernanceDimensions.Count);
        Assert.Contains(model.Assumptions, assumption => assumption.Name == "preset-assumption-1");
        Assert.Contains(model.KnownFailureModes, mode => mode == "wealth differences can still dominate access when scarcity is severe");
        Assert.Contains(model.KnownFailureModes, mode => mode == "wealth-weighted allocation can exclude low-wealth citizens during scarcity");

        var allocation = Assert.Single(model.GovernanceDimensions, dimension =>
            dimension.DimensionId == "allocation-mechanism");
        Assert.Equal("market-price-weighted", allocation.ValueId);
        Assert.Equal("Allocation priority follows Market Price Weighted.", allocation.Assumption);
        Assert.NotEmpty(allocation.KnownFailureModes);
    }
}
