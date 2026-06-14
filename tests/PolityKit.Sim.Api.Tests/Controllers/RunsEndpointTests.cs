using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Tests.TestHost;

namespace PolityKit.Sim.Api.Tests.Controllers;

public sealed class RunsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
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
    public async Task CreateSweepReturnsRunSummaryForEachParameterCombination()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var request = new ParameterSweepRequest
        {
            Scenario = "village-food-crisis",
            Seed = 333,
            Ticks = 4,
            Models = ["need-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["fixedWeight"] = 10
            },
            Sweep = new Dictionary<string, IReadOnlyList<double>>
            {
                ["needPriorityWeight"] = [1.0, 2.0],
                ["vulnerabilityPriorityWeight"] = [0.25, 0.5]
            }
        };

        var response = await client.PostAsJsonAsync("/api/runs/sweep", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sweep = await response.Content.ReadFromJsonAsync<ParameterSweepResponse>();
        Assert.NotNull(sweep);
        Assert.Equal("Village Food Crisis", sweep.ScenarioName);
        Assert.Equal(333, sweep.Seed);
        Assert.Equal(4, sweep.Ticks);
        Assert.Equal(4, sweep.RunCount);
        Assert.Equal(4, sweep.Runs.Count);
        Assert.NotEmpty(sweep.BestWorst);
        Assert.All(sweep.Runs, run =>
        {
            Assert.NotEqual(Guid.Empty, run.Run.Id);
            Assert.Equal(333, run.Run.Seed);
            Assert.Equal(["NeedBasedAllocation"], run.Run.Models);
            Assert.Equal(10, run.Parameters["fixedWeight"]);
            Assert.NotEmpty(run.FinalMetrics);
            Assert.Contains(run.FinalMetrics, metric => metric.Name == "Needs Met");
        });

        var runs = await client.GetFromJsonAsync<RunSummaryResponse[]>("/api/runs");
        Assert.NotNull(runs);
        Assert.Equal(4, runs.Length);
    }

    [Fact]
    public async Task CreateSweepWithEmptySweepReturnsBadRequestProblemDetails()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/runs/sweep", new ParameterSweepRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Sweep request is invalid.", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Contains("At least one sweep parameter is required.", problem.Detail);
    }

    [Fact]
    public async Task GetRunsReturnsCreatedRunsNewestFirst()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
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
        await using var isolatedFactory = CreateIsolatedFactory();
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
    public async Task RerunCreatesNewRunWithSameSeedTicksAndModels()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var created = await CreateRunAsync(client, new CreateRunRequest
        {
            Seed = 333,
            Ticks = 4,
            Models = ["need-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 2.0
            }
        });

        var response = await client.PostAsJsonAsync($"/api/runs/{created.Id}/rerun", new RerunRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var rerun = await response.Content.ReadFromJsonAsync<RunDetailResponse>();
        Assert.NotNull(rerun);
        Assert.NotEqual(created.Id, rerun.Id);
        Assert.Equal(created.ScenarioName, rerun.ScenarioName);
        Assert.Equal(created.Seed, rerun.Seed);
        Assert.Equal(created.Ticks, rerun.Ticks);

        var model = Assert.Single(rerun.Models);
        Assert.Equal("NeedBasedAllocation", model.ModelName);
        Assert.Equal(
            created.Models.Single().FinalMetrics.Select(metric => (metric.Name, metric.Value)).ToArray(),
            model.FinalMetrics.Select(metric => (metric.Name, metric.Value)).ToArray());
    }

    [Fact]
    public async Task RerunAppliesOverridesButKeepsOriginalSeed()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var created = await CreateRunAsync(client, new CreateRunRequest
        {
            Seed = 333,
            Ticks = 4,
            Models = ["need-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 1.0
            }
        });

        var response = await client.PostAsJsonAsync($"/api/runs/{created.Id}/rerun", new RerunRequest
        {
            Ticks = 3,
            Models = ["market-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["wealthPriorityWeight"] = 1.5
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var rerun = await response.Content.ReadFromJsonAsync<RunDetailResponse>();
        Assert.NotNull(rerun);
        Assert.Equal(333, rerun.Seed);
        Assert.Equal(3, rerun.Ticks);

        var model = Assert.Single(rerun.Models);
        Assert.Equal("MarketBasedAllocation", model.ModelName);
        Assert.All(model.FinalMetrics, metric => Assert.Equal(2, metric.Tick));
    }

    [Fact]
    public async Task RerunWithUnknownModelReturnsBadRequestProblemDetails()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var created = await CreateRunAsync(client, new CreateRunRequest());

        var response = await client.PostAsJsonAsync($"/api/runs/{created.Id}/rerun", new RerunRequest
        {
            Models = ["unknown-model"]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Run request is invalid.", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Unknown model 'unknown-model'.", problem.Detail);
    }

    [Fact]
    public async Task GetRunMetricsReturnsMetricsForCreatedRun()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
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
        await using var isolatedFactory = CreateIsolatedFactory();
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

    [Fact]
    public async Task GetRunDashboardReturnsDashboardReadyPayload()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var created = await CreateRunAsync(client, new CreateRunRequest
        {
            Ticks = 8,
            Models = ["need-based-allocation"]
        });

        var dashboard = await client.GetFromJsonAsync<RunDashboardResponse>($"/api/runs/{created.Id}/dashboard");

        Assert.NotNull(dashboard);
        Assert.Equal(created.Id, dashboard.Id);
        Assert.Equal("Village Food Crisis", dashboard.ScenarioName);
        Assert.Equal(8, dashboard.Ticks);
        Assert.NotEmpty(dashboard.Metrics);
        Assert.NotEmpty(dashboard.Events);
        Assert.Equal("Village Food Crisis", dashboard.Summary.ScenarioName);

        var model = Assert.Single(dashboard.Summary.Models);
        Assert.Equal("NeedBasedAllocation", model.ModelName);
        Assert.NotEmpty(model.EventCountsByType);
        Assert.NotEmpty(model.FinalMetrics);
    }

    [Fact]
    public async Task CompareRunsReturnsSideBySideMetricDeltas()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var baseline = await CreateRunAsync(client, new CreateRunRequest
        {
            Seed = 333,
            Ticks = 4,
            Models = ["need-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 1.0
            }
        });
        var comparison = await CreateRunAsync(client, new CreateRunRequest
        {
            Seed = 333,
            Ticks = 4,
            Models = ["need-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 2.0
            }
        });

        var response = await client.GetAsync($"/api/runs/{baseline.Id}/compare/{comparison.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var comparisonResponse = await response.Content.ReadFromJsonAsync<RunComparisonResponse>();
        Assert.NotNull(comparisonResponse);
        Assert.Equal(baseline.Id, comparisonResponse.Baseline.Id);
        Assert.Equal(comparison.Id, comparisonResponse.Comparison.Id);
        Assert.NotEmpty(comparisonResponse.MetricDeltas);
        Assert.All(comparisonResponse.MetricDeltas, delta =>
        {
            Assert.Equal("NeedBasedAllocation", delta.Model);
            Assert.NotNull(delta.BaselineValue);
            Assert.NotNull(delta.ComparisonValue);
            Assert.NotEqual("unavailable", delta.Direction);
        });
        Assert.Contains(comparisonResponse.MetricDeltas, delta => delta.Metric == "Needs Met");
    }

    [Theory]
    [InlineData("/api/runs/{0}")]
    [InlineData("/api/runs/{0}/metrics")]
    [InlineData("/api/runs/{0}/events")]
    [InlineData("/api/runs/{0}/dashboard")]
    public async Task RunLookupEndpointsReturnNotFoundForMissingRun(string routeTemplate)
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var route = string.Format(routeTemplate, Guid.NewGuid());

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompareRunsReturnsNotFoundWhenEitherRunIsMissing()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var created = await CreateRunAsync(client, new CreateRunRequest());

        var missingBaseline = await client.GetAsync($"/api/runs/{Guid.NewGuid()}/compare/{created.Id}");
        var missingComparison = await client.GetAsync($"/api/runs/{created.Id}/compare/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, missingBaseline.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingComparison.StatusCode);
    }

    [Fact]
    public async Task RerunReturnsNotFoundForMissingRun()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/runs/{Guid.NewGuid()}/rerun", new RerunRequest());

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
