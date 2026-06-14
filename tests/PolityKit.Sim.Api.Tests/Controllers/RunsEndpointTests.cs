using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using PolityKit.Sim.Api;
using PolityKit.Sim.Api.Contracts;

namespace PolityKit.Sim.Api.Tests.Controllers;

public sealed class RunsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public RunsEndpointTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CreateRunWithDefaultsReturnsCreatedRunDetail()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/runs", new CreateRunRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var run = await response.Content.ReadFromJsonAsync<RunDetailResponse>();
        Assert.NotNull(run);
        Assert.NotEqual(Guid.Empty, run.Id);
        Assert.Equal("Village Food Crisis", run.ScenarioName);
        Assert.Equal(12345, run.Seed);
        Assert.Equal(120, run.Ticks);

        var modelNames = run.Models.Select(model => model.ModelName).ToArray();
        Assert.Contains("NeedBasedAllocation", modelNames);
        Assert.Contains("MarketBasedAllocation", modelNames);
        Assert.Contains("HierarchyBasedAllocation", modelNames);
        Assert.All(run.Models, model =>
        {
            Assert.Equal("0.1.0", model.ModelVersion);
            Assert.NotEmpty(model.FinalMetrics);
            Assert.Contains(model.FinalMetrics, metric => metric.Name == "Needs Met");
        });
    }

    [Fact]
    public async Task CreateRunWithOverridesReturnsSelectedModelAndRunSettings()
    {
        var client = factory.CreateClient();
        var request = new CreateRunRequest
        {
            Scenario = "village-food-crisis",
            Seed = 777,
            Ticks = 5,
            Models = ["need-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 2.0,
                ["vulnerabilityPriorityWeight"] = 0.25
            }
        };

        var response = await client.PostAsJsonAsync("/api/runs", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var run = await response.Content.ReadFromJsonAsync<RunDetailResponse>();
        Assert.NotNull(run);
        Assert.Equal("Village Food Crisis", run.ScenarioName);
        Assert.Equal(777, run.Seed);
        Assert.Equal(5, run.Ticks);

        var model = Assert.Single(run.Models);
        Assert.Equal("NeedBasedAllocation", model.ModelName);
        Assert.All(model.FinalMetrics, metric => Assert.Equal(4, metric.Tick));
    }

    [Fact]
    public async Task CreateRunWithUnknownModelReturnsBadRequestProblemDetails()
    {
        var client = factory.CreateClient();
        var request = new CreateRunRequest
        {
            Models = ["unknown-model"]
        };

        var response = await client.PostAsJsonAsync("/api/runs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Run request is invalid.", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Unknown model 'unknown-model'.", problem.Detail);
    }
}
