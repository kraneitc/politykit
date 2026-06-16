using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Tests.TestHost;
using AiAnalysisArtifact = PolityKit.Sim.Analysis.AiAnalysisArtifact;
using AiAnalysisKind = PolityKit.Sim.Analysis.AiAnalysisKind;
using AiAnalysisRequest = PolityKit.Sim.Analysis.AiAnalysisRequest;
using AiAnalysisResult = PolityKit.Sim.Analysis.AiAnalysisResult;
using AiAnalysisStatus = PolityKit.Sim.Analysis.AiAnalysisStatus;
using AiBatchAnomaly = PolityKit.Sim.Analysis.AiBatchAnomaly;
using AiBatchAnomalyArtifact = PolityKit.Sim.Analysis.AiBatchAnomalyArtifact;
using AiBatchAnomalyReport = PolityKit.Sim.Analysis.AiBatchAnomalyReport;
using AiModelCritique = PolityKit.Sim.Analysis.AiModelCritique;
using AiModelCritiqueArtifact = PolityKit.Sim.Analysis.AiModelCritiqueArtifact;
using AiModelCritiqueItem = PolityKit.Sim.Analysis.AiModelCritiqueItem;
using AiScenarioSuggestionArtifact = PolityKit.Sim.Analysis.AiScenarioSuggestionArtifact;
using AiScenarioSuggestionDraft = PolityKit.Sim.Analysis.AiScenarioSuggestionDraft;
using IAiAnalysisProvider = PolityKit.Sim.Analysis.IAiAnalysisProvider;

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
    public async Task CreateRunAcceptsGovernancePresetStableId()
    {
        var client = factory.CreateClient();
        var request = new CreateRunRequest
        {
            Scenario = "village-food-crisis",
            Ticks = 5,
            Models = ["regulated-market"]
        };

        var response = await client.PostAsJsonAsync("/api/runs", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var run = await response.Content.ReadFromJsonAsync<RunDetailResponse>();
        Assert.NotNull(run);
        var model = Assert.Single(run.Models);
        Assert.Equal("CompositeGovernance:regulated-market", model.ModelName);
        Assert.NotEmpty(model.FinalMetrics);
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
    public async Task CreateRunWithScenarioPathReturnsBadRequestProblemDetails()
    {
        var path = Path.GetTempFileName();
        var client = factory.CreateClient();

        try
        {
            var response = await client.PostAsJsonAsync("/api/runs", new CreateRunRequest
            {
                Scenario = path
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal("Run request is invalid.", problem.Title);
            Assert.Equal(400, problem.Status);
            Assert.Contains("was not found.", problem.Detail);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CreateRunWithInvalidTicksReturnsBadRequestProblemDetails()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/runs", new CreateRunRequest
        {
            Ticks = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Run request is invalid.", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Scenario ticks must be greater than zero.", problem.Detail);
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
        Assert.NotEmpty(sweep.Sensitivity.Metrics);
        Assert.Contains(sweep.Sensitivity.Metrics, metric =>
            metric.Model == "NeedBasedAllocation"
            && metric.Metric == "Needs Met"
            && metric.Parameters.Any(parameter => parameter.Parameter == "needPriorityWeight"));
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
    public async Task CreateSweepWithInvalidTicksReturnsBadRequestProblemDetails()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/runs/sweep", new ParameterSweepRequest
        {
            Ticks = -1,
            Sweep = new Dictionary<string, IReadOnlyList<double>>
            {
                ["needPriorityWeight"] = [1.0]
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Sweep request is invalid.", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Scenario ticks must be greater than zero.", problem.Detail);
    }

    [Fact]
    public async Task CreateStressReturnsRunSummaryForEachStressRun()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var request = new StressSweepRequest
        {
            GridName = "endpoint-grid",
            Scenarios = ["village-food-crisis"],
            Seeds = [333, 444],
            Ticks = 4,
            Models = ["need-based-allocation", "market-based-allocation"],
            Sweep = new Dictionary<string, IReadOnlyList<double>>
            {
                ["needPriorityWeight"] = [1.0]
            }
        };

        var response = await client.PostAsJsonAsync("/api/runs/stress", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stress = await response.Content.ReadFromJsonAsync<StressSweepResponse>();
        Assert.NotNull(stress);
        Assert.Equal("endpoint-grid", stress.GridName);
        Assert.Equal(4, stress.RunCount);
        Assert.Equal(4, stress.Runs.Count);
        Assert.Equal(["Village Food Crisis"], stress.Scenarios);
        Assert.Equal([333, 444], stress.Seeds);
        Assert.Equal(["NeedBasedAllocation", "MarketBasedAllocation"], stress.Models);
        Assert.NotEmpty(stress.BestWorst);
        Assert.NotEmpty(stress.CollapseEvents);
        Assert.NotEmpty(stress.Sensitivity.Metrics);
        Assert.Contains(stress.Sensitivity.Metrics, metric =>
            metric.ScenarioName == "Village Food Crisis"
            && metric.Parameters.Any(parameter => parameter.Parameter == "needPriorityWeight"));
        Assert.Equal(["MarketBasedAllocation", "NeedBasedAllocation"], stress.ModelRobustness.Select(summary => summary.Model).Order().ToArray());
        Assert.All(stress.ModelRobustness, summary =>
        {
            Assert.Equal(["Village Food Crisis"], summary.ScenariosTested);
            Assert.Equal([333, 444], summary.SeedsTested);
            Assert.Equal(2, summary.RunsCompleted);
            Assert.Equal("needPriorityWeight", summary.MostSensitiveParameter);
        });
        Assert.All(stress.Runs, run =>
        {
            Assert.NotEqual(Guid.Empty, run.Run.Id);
            Assert.Equal("Village Food Crisis", run.ScenarioName);
            Assert.Equal(4, run.Ticks);
            Assert.Single(run.Run.Models);
            Assert.NotEmpty(run.FinalMetrics);
            Assert.NotEmpty(run.CollapseEvents);
        });

        var runs = await client.GetFromJsonAsync<RunSummaryResponse[]>("/api/runs");
        Assert.NotNull(runs);
        Assert.Equal(4, runs.Length);
    }

    [Fact]
    public async Task CreateStressComparesBaselinesAndGovernancePresets()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var request = new StressSweepRequest
        {
            GridName = "baseline-preset-comparison",
            Scenarios = ["village-food-crisis"],
            Seeds = [111, 222],
            Ticks = 4,
            Models =
            [
                "need-based-allocation",
                "market-based-allocation",
                "hierarchy-based-allocation",
                "participatory-commons",
                "regulated-market"
            ],
            Sweep = new Dictionary<string, IReadOnlyList<double>>
            {
                ["needWeightMultiplier"] = [0.8, 1.2]
            }
        };

        var response = await client.PostAsJsonAsync("/api/runs/stress", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stress = await response.Content.ReadFromJsonAsync<StressSweepResponse>();
        Assert.NotNull(stress);
        Assert.Equal("baseline-preset-comparison", stress.GridName);
        Assert.Equal(20, stress.RunCount);
        Assert.Equal(20, stress.Runs.Count);
        Assert.Equal(
            [
                "NeedBasedAllocation",
                "MarketBasedAllocation",
                "HierarchyBasedAllocation",
                "CompositeGovernance:participatory-commons",
                "CompositeGovernance:regulated-market"
            ],
            stress.Models);
        Assert.NotEmpty(stress.BestWorst);
        Assert.NotEmpty(stress.Sensitivity.Metrics);
        Assert.Equal(stress.Models.Order().ToArray(), stress.ModelRobustness.Select(summary => summary.Model).Order().ToArray());
        Assert.All(stress.ModelRobustness, summary =>
        {
            Assert.Equal(["Village Food Crisis"], summary.ScenariosTested);
            Assert.Equal([111, 222], summary.SeedsTested);
            Assert.Equal(4, summary.RunsCompleted);
            Assert.Equal("needWeightMultiplier", summary.MostSensitiveParameter);
        });
        Assert.All(stress.Runs, run =>
        {
            Assert.Single(run.Run.Models);
            Assert.NotEmpty(run.FinalMetrics);
            Assert.Contains(run.FinalMetrics, metric => metric.Name == "Needs Met");
        });
    }

    [Fact]
    public async Task CreateStressOverLimitReturnsBadRequestProblemDetails()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var request = new StressSweepRequest
        {
            Scenarios = ["village-food-crisis"],
            Seeds = [1, 2],
            Models = ["need-based-allocation", "market-based-allocation"],
            Sweep = new Dictionary<string, IReadOnlyList<double>>
            {
                ["needPriorityWeight"] = [1.0, 2.0]
            },
            MaxRuns = 7
        };

        var response = await client.PostAsJsonAsync("/api/runs/stress", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Stress sweep request is invalid.", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Stress sweep would create 8 runs; the maximum is 7.", problem.Detail);
    }

    [Fact]
    public async Task CreateStressWithInvalidTicksReturnsBadRequestProblemDetails()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/runs/stress", new StressSweepRequest
        {
            Scenarios = ["village-food-crisis"],
            Seeds = [1],
            Ticks = 0,
            Models = ["need-based-allocation"]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Stress sweep request is invalid.", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Scenario ticks must be greater than zero.", problem.Detail);
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
    public async Task RerunWithInvalidTicksReturnsBadRequestProblemDetails()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var created = await CreateRunAsync(client, new CreateRunRequest());

        var response = await client.PostAsJsonAsync($"/api/runs/{created.Id}/rerun", new RerunRequest
        {
            Ticks = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Run request is invalid.", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Contains("Scenario ticks must be greater than zero.", problem.Detail);
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

    [Fact]
    public async Task CreateAiSummaryUsesProviderAndRunSummaryContext()
    {
        var provider = new RecordingAiProvider(new AiAnalysisResult(
            AiAnalysisStatus.Succeeded,
            "The run stayed mostly stable while trust moved. This advisory summary does not claim real-world predictive validity.",
            [],
            ["fake warning"],
            ["PolityKit.Sim.Cli run --scenario village-food-crisis"]));
        await using var isolatedFactory = CreateIsolatedFactory().WithAiProvider(provider);
        var client = isolatedFactory.CreateClient();
        var run = await CreateRunAsync(client, new CreateRunRequest
        {
            Scenario = "village-food-crisis",
            Ticks = 5,
            Models = ["need-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 2.0
            }
        });

        var response = await client.PostAsync($"/api/runs/{run.Id}/ai-summary", null);

        response.EnsureSuccessStatusCode();
        var artifact = await response.Content.ReadFromJsonAsync<AiAnalysisArtifact>();
        Assert.NotNull(artifact);
        Assert.Equal(AiAnalysisKind.RunSummary, artifact.Kind);
        Assert.Equal(AiAnalysisStatus.Succeeded, artifact.Result.Status);
        Assert.Equal("The run stayed mostly stable while trust moved. This advisory summary does not claim real-world predictive validity.", artifact.Result.GeneratedText);
        Assert.True(artifact.AiAnalysis.Used);
        Assert.Equal("recording-provider", artifact.AiAnalysis.ProviderName);
        Assert.Equal("recording-model", artifact.AiAnalysis.ProviderModel);
        Assert.Equal([run.Id], artifact.AiAnalysis.InputRunIds);
        Assert.Equal(["Village Food Crisis"], artifact.AiAnalysis.ScenarioNames);
        Assert.Equal(["NeedBasedAllocation"], artifact.AiAnalysis.ModelNames);
        Assert.Equal([12345], artifact.AiAnalysis.Seeds);
        Assert.NotNull(provider.LastRequest);
        Assert.Equal(AiAnalysisKind.RunSummary, provider.LastRequest.Kind);
        Assert.Contains("\"scenarioName\": \"Village Food Crisis\"", provider.LastRequest.Context);
        Assert.Contains("\"name\": \"needPriorityWeight\"", provider.LastRequest.Context);
        Assert.Contains("NeedBasedAllocation:", provider.LastRequest.Context);
    }

    [Fact]
    public async Task CreateScenarioSuggestionReturnsValidatedDraft()
    {
        var provider = new RecordingAiProvider(new AiAnalysisResult(
            AiAnalysisStatus.Succeeded,
            "Review this draft before saving it as a scenario file.",
            [],
            ["fake warning"],
            ["PolityKit.Sim.Cli suggest-scenario --bundle runs/example"],
            new AiScenarioSuggestionDraft(
                new PolityKit.Sim.Core.Scenarios.ScenarioDefinition
                {
                    Name = "Draft Food Stress Follow-up",
                    Seed = 12345,
                    Ticks = 20,
                    InitialPopulation = 25,
                    InitialResources = new PolityKit.Sim.Core.World.ResourcePool
                    {
                        Food = 50,
                        Medicine = 20,
                        Housing = 20,
                        AdminCapacity = 5,
                        ProductionCapacity = 5
                    },
                    Shocks =
                    [
                        new PolityKit.Sim.Core.Scenarios.ShockDefinition
                        {
                            Tick = 5,
                            Type = "CropFailure",
                            Severity = 0.3
                        }
                    ]
                },
                ["Add a moderate crop failure."],
                "Probe recovery under a smaller shock.")));
        await using var isolatedFactory = CreateIsolatedFactory().WithAiProvider(provider);
        var client = isolatedFactory.CreateClient();
        var run = await CreateRunAsync(client, new CreateRunRequest
        {
            Scenario = "village-food-crisis",
            Ticks = 5,
            Models = ["need-based-allocation"]
        });

        var response = await client.PostAsync($"/api/runs/{run.Id}/scenario-suggestions", null);

        response.EnsureSuccessStatusCode();
        var artifact = await response.Content.ReadFromJsonAsync<AiScenarioSuggestionArtifact>();
        Assert.NotNull(artifact);
        Assert.True(artifact.CanSave);
        Assert.True(artifact.Validation.IsValid);
        Assert.Empty(artifact.Validation.Errors);
        Assert.NotNull(artifact.Draft);
        Assert.True(artifact.Draft.IsDraft);
        Assert.Equal("Draft Food Stress Follow-up", artifact.Draft.Scenario.Name);
        Assert.Equal(AiAnalysisKind.ScenarioSuggestion, artifact.Analysis.Kind);
        Assert.True(artifact.Analysis.AiAnalysis.Used);
        Assert.Equal([run.Id], artifact.Analysis.AiAnalysis.InputRunIds);
        Assert.NotNull(provider.LastRequest);
        Assert.Equal(AiAnalysisKind.ScenarioSuggestion, provider.LastRequest.Kind);
        Assert.Contains("\"sourceType\": \"scenario-suggestion\"", provider.LastRequest.Context);
        Assert.Contains("Shock tick must be within", provider.LastRequest.Context);
        Assert.Contains("\"observedFailures\"", provider.LastRequest.Context);
    }

    [Fact]
    public async Task CreateScenarioSuggestionReportsInvalidProviderDraftWithoutAcceptingIt()
    {
        var provider = new RecordingAiProvider(new AiAnalysisResult(
            AiAnalysisStatus.Succeeded,
            "This draft is intentionally invalid.",
            [],
            [],
            [],
            new PolityKit.Sim.Core.Scenarios.ScenarioDefinition
            {
                Name = "",
                Ticks = -1,
                InitialPopulation = -5
            }));
        await using var isolatedFactory = CreateIsolatedFactory().WithAiProvider(provider);
        var client = isolatedFactory.CreateClient();
        var run = await CreateRunAsync(client, new CreateRunRequest
        {
            Scenario = "village-food-crisis",
            Ticks = 5,
            Models = ["need-based-allocation"]
        });

        var response = await client.PostAsync($"/api/runs/{run.Id}/scenario-suggestions", null);

        response.EnsureSuccessStatusCode();
        var artifact = await response.Content.ReadFromJsonAsync<AiScenarioSuggestionArtifact>();
        Assert.NotNull(artifact);
        Assert.False(artifact.CanSave);
        Assert.False(artifact.Validation.IsValid);
        Assert.Contains("Scenario name is required.", artifact.Validation.Errors);
        Assert.Contains("Scenario ticks must be greater than zero.", artifact.Validation.Errors);
        Assert.Contains("Initial population cannot be negative.", artifact.Validation.Errors);
    }

    [Fact]
    public async Task CreateModelCritiqueUsesProviderAndSelectedPresetManifest()
    {
        var provider = new RecordingAiProvider(new AiAnalysisResult(
            AiAnalysisStatus.Succeeded,
            "Review this critique as advisory model feedback, not proof that the model is correct or incorrect.",
            [],
            ["fake warning"],
            ["PolityKit.Sim.Cli critique-model --bundle runs/example --model regulated-market"],
            new AiModelCritique(
                [
                    new AiModelCritiqueItem(
                        "Market access risk",
                        "The regulated-market preset still relies on wealth-shaped access during scarcity.",
                        ["Allocation mechanism: Market Price Weighted"])
                ],
                [
                    new AiModelCritiqueItem(
                        "Administrative load pressure",
                        "Oversight and appeal dimensions may raise administrative load.",
                        ["Administrative Load final metric"])
                ],
                ["Run a stress sweep with needWeightMultiplier values around the default."],
                ["Document which preset assumptions were not directly tested by this run."])));
        await using var isolatedFactory = CreateIsolatedFactory().WithAiProvider(provider);
        var client = isolatedFactory.CreateClient();
        var run = await CreateRunAsync(client, new CreateRunRequest
        {
            Scenario = "village-food-crisis",
            Ticks = 5,
            Models = ["regulated-market"]
        });

        var response = await client.PostAsync($"/api/runs/{run.Id}/ai/model-critique?model=regulated-market", null);

        response.EnsureSuccessStatusCode();
        var artifact = await response.Content.ReadFromJsonAsync<AiModelCritiqueArtifact>();
        Assert.NotNull(artifact);
        Assert.Equal(AiAnalysisKind.ModelCritique, artifact.Analysis.Kind);
        Assert.Equal(AiAnalysisStatus.Succeeded, artifact.Analysis.Result.Status);
        Assert.True(artifact.Analysis.AiAnalysis.Used);
        Assert.Equal([run.Id], artifact.Analysis.AiAnalysis.InputRunIds);
        Assert.Equal(["CompositeGovernance:regulated-market"], artifact.Analysis.AiAnalysis.ModelNames);
        Assert.NotNull(artifact.Critique);
        Assert.NotEmpty(artifact.Critique.AssumptionRisks);
        Assert.NotEmpty(artifact.Critique.ObservedFailureModes);
        Assert.NotEmpty(artifact.Critique.SuggestedTests);
        Assert.NotEmpty(artifact.Critique.SuggestedDocumentationUpdates);
        Assert.NotNull(provider.LastRequest);
        Assert.Equal(AiAnalysisKind.ModelCritique, provider.LastRequest.Kind);
        Assert.Contains("\"sourceType\": \"model-critique\"", provider.LastRequest.Context);
        Assert.Contains("\"governanceDimensions\"", provider.LastRequest.Context);
        Assert.Contains("not proof that a model is correct or incorrect", provider.LastRequest.Context);
    }

    [Fact]
    public async Task CreateStressAnomaliesUsesProviderAndMixedBaselinePresetContext()
    {
        var provider = new RecordingAiProvider(new AiAnalysisResult(
            AiAnalysisStatus.Succeeded,
            "Review these anomaly candidates against the deterministic stress summary.",
            [],
            ["fake warning"],
            ["PolityKit.Sim.Cli ai-anomalies --stress-summary runs/example/stress-summary.json"],
            new AiBatchAnomalyReport(
                [
                    new AiBatchAnomaly(
                        null,
                        "CompositeGovernance:regulated-market",
                        "Village Food Crisis",
                        111,
                        "Administrative Load",
                        12,
                        "Administrative load was high for the governance preset under this stress grid.")
                ])));
        await using var isolatedFactory = CreateIsolatedFactory().WithAiProvider(provider);
        var client = isolatedFactory.CreateClient();
        var request = new StressSweepRequest
        {
            GridName = "anomaly-mixed-grid",
            Scenarios = ["village-food-crisis"],
            Seeds = [111],
            Ticks = 4,
            Models = ["need-based-allocation", "regulated-market"],
            Sweep = new Dictionary<string, IReadOnlyList<double>>
            {
                ["needWeightMultiplier"] = [0.8, 1.2]
            }
        };

        var response = await client.PostAsJsonAsync("/api/runs/stress/ai/anomalies", request);

        response.EnsureSuccessStatusCode();
        var artifact = await response.Content.ReadFromJsonAsync<AiBatchAnomalyArtifact>();
        Assert.NotNull(artifact);
        Assert.True(artifact.CanUse);
        Assert.True(artifact.Validation.IsValid);
        Assert.Empty(artifact.Validation.Errors);
        Assert.Equal(AiAnalysisKind.BatchAnomalyReport, artifact.Analysis.Kind);
        Assert.True(artifact.Analysis.AiAnalysis.Used);
        Assert.Equal(["CompositeGovernance:regulated-market", "NeedBasedAllocation"], artifact.Analysis.AiAnalysis.ModelNames);
        Assert.Equal(["Village Food Crisis"], artifact.Analysis.AiAnalysis.ScenarioNames);
        Assert.Equal([111], artifact.Analysis.AiAnalysis.Seeds);
        Assert.NotNull(artifact.Report);
        var anomaly = Assert.Single(artifact.Report.Anomalies);
        Assert.Equal("CompositeGovernance:regulated-market", anomaly.Model);
        Assert.NotNull(provider.LastRequest);
        Assert.Equal(AiAnalysisKind.BatchAnomalyReport, provider.LastRequest.Kind);
        Assert.Contains("\"sourceType\": \"batch-anomaly\"", provider.LastRequest.Context);
        Assert.Contains("NeedBasedAllocation", provider.LastRequest.Context);
        Assert.Contains("CompositeGovernance:regulated-market", provider.LastRequest.Context);
        Assert.Contains("\"modelRobustness\"", provider.LastRequest.Context);
    }

    [Fact]
    public async Task RunSweepStressAndComparisonWorkWithoutAiConfiguration()
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();

        var baseline = await CreateRunAsync(client, new CreateRunRequest
        {
            Seed = 333,
            Ticks = 3,
            Models = ["need-based-allocation"]
        });
        var comparison = await CreateRunAsync(client, new CreateRunRequest
        {
            Seed = 333,
            Ticks = 3,
            Models = ["market-based-allocation"]
        });

        var sweepResponse = await client.PostAsJsonAsync("/api/runs/sweep", new ParameterSweepRequest
        {
            Scenario = "village-food-crisis",
            Seed = 333,
            Ticks = 3,
            Models = ["need-based-allocation"],
            Sweep = new Dictionary<string, IReadOnlyList<double>>
            {
                ["needPriorityWeight"] = [1.0]
            }
        });
        var stressResponse = await client.PostAsJsonAsync("/api/runs/stress", new StressSweepRequest
        {
            Scenarios = ["village-food-crisis"],
            Seeds = [333],
            Ticks = 3,
            Models = ["need-based-allocation"],
            Sweep = new Dictionary<string, IReadOnlyList<double>>
            {
                ["needPriorityWeight"] = [1.0]
            }
        });
        var comparisonResponse = await client.GetAsync($"/api/runs/{baseline.Id}/compare/{comparison.Id}");

        sweepResponse.EnsureSuccessStatusCode();
        stressResponse.EnsureSuccessStatusCode();
        comparisonResponse.EnsureSuccessStatusCode();

        var sweep = await sweepResponse.Content.ReadFromJsonAsync<ParameterSweepResponse>();
        var stress = await stressResponse.Content.ReadFromJsonAsync<StressSweepResponse>();
        var runComparison = await comparisonResponse.Content.ReadFromJsonAsync<RunComparisonResponse>();

        Assert.NotNull(sweep);
        Assert.NotNull(stress);
        Assert.NotNull(runComparison);
        AssertAiNotUsed(baseline.AiAnalysis);
        AssertAiNotUsed(comparison.AiAnalysis);
        AssertAiNotUsed(sweep.AiAnalysis);
        AssertAiNotUsed(stress.AiAnalysis);
        AssertAiNotUsed(runComparison.AiAnalysis);
        AssertAiNotUsed(runComparison.Baseline.AiAnalysis);
        AssertAiNotUsed(runComparison.Comparison.AiAnalysis);
    }

    [Theory]
    [InlineData("/api/runs/{0}")]
    [InlineData("/api/runs/{0}/metrics")]
    [InlineData("/api/runs/{0}/events")]
    [InlineData("/api/runs/{0}/dashboard")]
    [InlineData("/api/runs/{0}/ai-summary")]
    [InlineData("/api/runs/{0}/scenario-suggestions")]
    [InlineData("/api/runs/{0}/ai/model-critique")]
    public async Task RunLookupEndpointsReturnNotFoundForMissingRun(string routeTemplate)
    {
        await using var isolatedFactory = CreateIsolatedFactory();
        var client = isolatedFactory.CreateClient();
        var route = string.Format(routeTemplate, Guid.NewGuid());

        var response = route.EndsWith("/ai-summary", StringComparison.Ordinal)
            || route.EndsWith("/scenario-suggestions", StringComparison.Ordinal)
            || route.EndsWith("/model-critique", StringComparison.Ordinal)
            ? await client.PostAsync(route, null)
            : await client.GetAsync(route);

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

    private static void AssertAiNotUsed(PolityKit.Sim.Analysis.AiAnalysisUsage usage)
    {
        Assert.False(usage.Used);
        Assert.Empty(usage.InputRunIds);
        Assert.Empty(usage.InputFiles);
        Assert.Null(usage.ProviderName);
        Assert.Null(usage.ProviderModel);
    }

    private sealed class RecordingAiProvider(AiAnalysisResult result) : IAiAnalysisProvider
    {
        public AiAnalysisRequest? LastRequest { get; private set; }

        public string ProviderName => "recording-provider";

        public string ProviderModel => "recording-model";

        public Task<AiAnalysisResult> AnalyzeAsync(
            AiAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }
}
