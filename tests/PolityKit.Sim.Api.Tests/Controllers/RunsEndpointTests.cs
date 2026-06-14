using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using PolityKit.Sim.Api;
using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Tests.TestHost;

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

    [Fact]
    public async Task GetRunsReturnsCreatedRunsNewestFirst()
    {
        using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();

        var first = await CreateRunAsync(client, new CreateRunRequest
        {
            Seed = 111,
            Ticks = 2,
            Models = ["need-based-allocation"]
        });
        await Task.Delay(10);
        var second = await CreateRunAsync(client, new CreateRunRequest
        {
            Seed = 222,
            Ticks = 3,
            Models = ["market-based-allocation"]
        });

        var runs = await client.GetFromJsonAsync<RunSummaryResponse[]>("/api/runs");

        Assert.NotNull(runs);
        Assert.Equal([second.Id, first.Id], runs.Select(run => run.Id).ToArray());
        Assert.Equal(222, runs[0].Seed);
        Assert.Equal(3, runs[0].Ticks);
        Assert.Equal(["MarketBasedAllocation"], runs[0].Models);
        Assert.Equal(111, runs[1].Seed);
        Assert.Equal(2, runs[1].Ticks);
        Assert.Equal(["NeedBasedAllocation"], runs[1].Models);
    }

    [Fact]
    public async Task GetRunReturnsCreatedRunById()
    {
        using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var created = await CreateRunAsync(client, new CreateRunRequest
        {
            Seed = 333,
            Ticks = 4,
            Models = ["hierarchy-based-allocation"]
        });

        var fetched = await client.GetFromJsonAsync<RunDetailResponse>($"/api/runs/{created.Id}");

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal("Village Food Crisis", fetched.ScenarioName);
        Assert.Equal(333, fetched.Seed);
        Assert.Equal(4, fetched.Ticks);

        var model = Assert.Single(fetched.Models);
        Assert.Equal("HierarchyBasedAllocation", model.ModelName);
        Assert.NotEmpty(model.FinalMetrics);
    }

    [Fact]
    public async Task GetRunMetricsReturnsMetricsForCreatedRun()
    {
        using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var created = await CreateRunAsync(client, new CreateRunRequest
        {
            Ticks = 3,
            Models = ["need-based-allocation"]
        });

        var metrics = await client.GetFromJsonAsync<MetricResponse[]>($"/api/runs/{created.Id}/metrics");

        Assert.NotNull(metrics);
        Assert.NotEmpty(metrics);
        Assert.All(metrics, metric =>
        {
            Assert.Equal("NeedBasedAllocation", metric.Model);
            Assert.InRange(metric.Tick, 0, 2);
            Assert.False(string.IsNullOrWhiteSpace(metric.Name));
        });
        Assert.Contains(metrics, metric => metric.Name == "Needs Met");
        Assert.Contains(metrics, metric => metric.Name == "Trust");
    }

    [Fact]
    public async Task GetRunEventsReturnsEventsForCreatedRun()
    {
        using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var created = await CreateRunAsync(client, new CreateRunRequest
        {
            Ticks = 3,
            Models = ["need-based-allocation"]
        });

        var events = await client.GetFromJsonAsync<EventResponse[]>($"/api/runs/{created.Id}/events");

        Assert.NotNull(events);
        Assert.NotEmpty(events);
        Assert.All(events, simulationEvent =>
        {
            Assert.Equal("NeedBasedAllocation", simulationEvent.Model);
            Assert.InRange(simulationEvent.Tick, 0, 2);
            Assert.False(string.IsNullOrWhiteSpace(simulationEvent.Type));
        });
        Assert.Contains(events, simulationEvent => simulationEvent.Type == "ResourceAllocated");
    }

    [Theory]
    [InlineData("/api/runs/{0}")]
    [InlineData("/api/runs/{0}/metrics")]
    [InlineData("/api/runs/{0}/events")]
    public async Task RunLookupEndpointsReturnNotFoundForMissingRun(string routeTemplate)
    {
        using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var route = string.Format(routeTemplate, Guid.NewGuid());

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateIsolatedFactory()
    {
        return factory.WithIsolatedRunStore();
    }

    private static async Task<RunDetailResponse> CreateRunAsync(HttpClient client, CreateRunRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/runs", request);
        response.EnsureSuccessStatusCode();

        var run = await response.Content.ReadFromJsonAsync<RunDetailResponse>();
        Assert.NotNull(run);

        return run;
    }
}
