using PolityKit.Sim.Analysis;
using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Services;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Engine;
using PolityKit.Sim.Metrics;
using PolityKit.Sim.Models;
using PolityKit.Sim.Scenarios;
using ApiStressSweepRequest = PolityKit.Sim.Api.Contracts.StressSweepRequest;

namespace PolityKit.Sim.Api.Tests.Runs;

public sealed class SimulationRunServiceTests
{
    [Fact]
    public void CreateRunDefaultsToAllModelsMetricsAndEmptyParameters()
    {
        var engine = new RecordingSimulationEngine();
        var store = new InMemoryRunStore();
        var service = CreateService(engine, store);

        var storedRun = service.CreateRun(new CreateRunRequest
        {
            Scenario = "service-scenario"
        });

        Assert.Same(storedRun, store.Get(storedRun.Id));
        Assert.NotNull(engine.LastRequest);
        Assert.Equal("Service Scenario", engine.LastRequest.Scenario.Name);
        Assert.Contains("NeedBasedAllocation", engine.LastRequest.Models.Select(model => model.Name));
        Assert.Contains("MarketBasedAllocation", engine.LastRequest.Models.Select(model => model.Name));
        Assert.Contains("HierarchyBasedAllocation", engine.LastRequest.Models.Select(model => model.Name));
        Assert.Contains("CompositeGovernance:participatory-commons", engine.LastRequest.Models.Select(model => model.Name));
        Assert.Equal(["Needs Met", "Inequality", "Trust", "Severe Failures", "Administrative Load"], engine.LastRequest.Metrics.Select(metric => metric.Name).ToArray());
        Assert.Empty(engine.LastRequest.Parameters);
    }

    [Fact]
    public void CreateRunAppliesSeedTicksSelectedModelsAndParameters()
    {
        var engine = new RecordingSimulationEngine();
        var parameters = new Dictionary<string, double>
        {
            ["needPriorityWeight"] = 2.0
        };
        var service = CreateService(engine, new InMemoryRunStore());

        service.CreateRun(new CreateRunRequest
        {
            Scenario = "service-scenario",
            Seed = 777,
            Ticks = 6,
            Models = ["need-based-allocation"],
            Parameters = parameters
        });

        Assert.NotNull(engine.LastRequest);
        Assert.Equal(777, engine.LastRequest.Seed);
        Assert.Equal(777, engine.LastRequest.Scenario.Seed);
        Assert.Equal(6, engine.LastRequest.Scenario.Ticks);
        var model = Assert.Single(engine.LastRequest.Models);
        Assert.Equal("NeedBasedAllocation", model.Name);
        Assert.Same(parameters, engine.LastRequest.Parameters);
    }

    [Theory]
    [InlineData("regulated-market")]
    [InlineData("preset:regulated-market")]
    public void CreateRunAcceptsGovernancePresetSelectors(string selector)
    {
        var engine = new RecordingSimulationEngine();
        var service = CreateService(engine, new InMemoryRunStore());

        service.CreateRun(new CreateRunRequest
        {
            Scenario = "service-scenario",
            Models = [selector]
        });

        Assert.NotNull(engine.LastRequest);
        var model = Assert.Single(engine.LastRequest.Models);
        Assert.Equal("CompositeGovernance:regulated-market", model.Name);
    }

    [Fact]
    public void RerunUsesStoredScenarioSeedTicksModelsAndParameters()
    {
        var engine = new RecordingSimulationEngine();
        var store = new InMemoryRunStore();
        var service = CreateService(engine, store);
        var original = service.CreateRun(new CreateRunRequest
        {
            Scenario = "service-scenario",
            Seed = 777,
            Ticks = 6,
            Models = ["need-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 2.0
            }
        });

        var rerun = service.Rerun(original.Id, null);

        Assert.NotNull(rerun);
        Assert.NotEqual(original.Id, rerun.Id);
        Assert.Same(rerun, store.Get(rerun.Id));
        Assert.Equal(2, engine.Requests.Count);

        var request = engine.Requests[1];
        Assert.Equal("Service Scenario", request.Scenario.Name);
        Assert.Equal(777, request.Seed);
        Assert.Equal(777, request.Scenario.Seed);
        Assert.Equal(6, request.Scenario.Ticks);
        Assert.Equal(["NeedBasedAllocation"], request.Models.Select(model => model.Name).ToArray());
        Assert.Equal(2.0, request.Parameters["needPriorityWeight"]);
    }

    [Fact]
    public void RerunAllowsTickModelAndParameterOverridesWithoutChangingSeed()
    {
        var engine = new RecordingSimulationEngine();
        var service = CreateService(engine, new InMemoryRunStore());
        var original = service.CreateRun(new CreateRunRequest
        {
            Scenario = "service-scenario",
            Seed = 777,
            Ticks = 6,
            Models = ["need-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 2.0,
                ["vulnerabilityPriorityWeight"] = 0.25
            }
        });

        service.Rerun(original.Id, new RerunRequest
        {
            Ticks = 4,
            Models = ["market-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 3.0,
                ["wealthPriorityWeight"] = 1.5
            }
        });

        var request = engine.Requests[1];
        Assert.Equal(777, request.Seed);
        Assert.Equal(777, request.Scenario.Seed);
        Assert.Equal(4, request.Scenario.Ticks);
        Assert.Equal(["MarketBasedAllocation"], request.Models.Select(model => model.Name).ToArray());
        Assert.Equal(3.0, request.Parameters["needPriorityWeight"]);
        Assert.Equal(0.25, request.Parameters["vulnerabilityPriorityWeight"]);
        Assert.Equal(1.5, request.Parameters["wealthPriorityWeight"]);
    }

    [Fact]
    public void RerunReturnsNullForMissingRun()
    {
        var service = CreateService(new RecordingSimulationEngine(), new InMemoryRunStore());

        var rerun = service.Rerun(Guid.NewGuid(), null);

        Assert.Null(rerun);
    }

    [Fact]
    public void CreateSweepRunsAllParameterCombinations()
    {
        var engine = new RecordingSimulationEngine();
        var store = new InMemoryRunStore();
        var service = CreateService(engine, store);

        var response = service.CreateSweep(new ParameterSweepRequest
        {
            Scenario = "service-scenario",
            Seed = 777,
            Ticks = 6,
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
        });

        Assert.Equal("Service Scenario", response.ScenarioName);
        Assert.Equal(777, response.Seed);
        Assert.Equal(6, response.Ticks);
        Assert.Equal(4, response.RunCount);
        Assert.Equal(4, response.Runs.Count);
        Assert.NotEmpty(response.BestWorst);
        Assert.All(response.BestWorst, report =>
        {
            Assert.InRange(report.Best.RunIndex, 1, 4);
            Assert.InRange(report.Worst.RunIndex, 1, 4);
        });
        Assert.Equal(4, store.List().Count);
        Assert.Equal(4, engine.Requests.Count);
        Assert.All(response.Runs, run =>
        {
            Assert.Equal(777, run.Run.Seed);
            Assert.Equal(6, run.Run.Ticks);
            Assert.Equal(["NeedBasedAllocation"], run.Run.Models);
            Assert.Equal(10, run.Parameters["fixedWeight"]);
        });

        var combinations = response.Runs
            .Select(run => (Need: run.Parameters["needPriorityWeight"], Vulnerability: run.Parameters["vulnerabilityPriorityWeight"]))
            .ToHashSet();
        Assert.Equal(4, combinations.Count);
        Assert.Contains((1.0, 0.25), combinations);
        Assert.Contains((1.0, 0.5), combinations);
        Assert.Contains((2.0, 0.25), combinations);
        Assert.Contains((2.0, 0.5), combinations);
    }

    [Fact]
    public void CreateSweepRejectsEmptySweep()
    {
        var service = CreateService(new RecordingSimulationEngine(), new InMemoryRunStore());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.CreateSweep(new ParameterSweepRequest()));

        Assert.Equal("At least one sweep parameter is required.", exception.Message);
    }

    [Fact]
    public void CreateSweepRejectsParameterWithoutValues()
    {
        var service = CreateService(new RecordingSimulationEngine(), new InMemoryRunStore());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.CreateSweep(new ParameterSweepRequest
            {
                Sweep = new Dictionary<string, IReadOnlyList<double>>
                {
                    ["needPriorityWeight"] = []
                }
            }));

        Assert.Equal("Sweep parameter 'needPriorityWeight' must include at least one value.", exception.Message);
    }

    [Fact]
    public void CreateStressRunsScenarioSeedModelAndParameterCombinations()
    {
        var engine = new RecordingSimulationEngine();
        var store = new InMemoryRunStore();
        var service = CreateService(engine, store);

        var response = service.CreateStress(new ApiStressSweepRequest
        {
            Scenarios = ["service-scenario"],
            Seeds = [111, 222],
            Models = ["need-based-allocation", "market-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["fixedWeight"] = 10
            },
            Sweep = new Dictionary<string, IReadOnlyList<double>>
            {
                ["needPriorityWeight"] = [1.0, 2.0]
            }
        });

        Assert.Equal(8, response.RunCount);
        Assert.Equal(8, response.Runs.Count);
        Assert.Equal(8, engine.Requests.Count);
        Assert.Equal(8, store.List().Count);
        Assert.Equal(["NeedBasedAllocation", "MarketBasedAllocation"], response.Models);
        Assert.Equal([111, 222], response.Seeds);
        Assert.NotEmpty(response.BestWorst);
        Assert.NotEmpty(response.CollapseEvents);
        Assert.Equal(["MarketBasedAllocation", "NeedBasedAllocation"], response.ModelRobustness.Select(summary => summary.Model).Order().ToArray());
        Assert.All(response.ModelRobustness, summary =>
        {
            Assert.Equal(["Service Scenario"], summary.ScenariosTested);
            Assert.Equal([111, 222], summary.SeedsTested);
            Assert.Equal(4, summary.RunsCompleted);
            Assert.Equal("needPriorityWeight", summary.MostSensitiveParameter);
        });
        Assert.All(response.Runs, run =>
        {
            Assert.NotEqual(Guid.Empty, run.Run.Id);
            Assert.Equal("Service Scenario", run.ScenarioName);
            Assert.Equal(10, run.Parameters["fixedWeight"]);
            Assert.Single(run.Run.Models);
            Assert.Single(run.FinalMetrics);
            Assert.NotEmpty(run.CollapseEvents);
        });

        var dimensions = response.Runs
            .Select(run => (run.Seed, run.Model, Weight: run.Parameters["needPriorityWeight"]))
            .ToHashSet();
        Assert.Equal(8, dimensions.Count);
        Assert.Contains((111, "NeedBasedAllocation", 1.0), dimensions);
        Assert.Contains((222, "MarketBasedAllocation", 2.0), dimensions);
    }

    [Fact]
    public void CreateStressUsesConfiguredFailureCriteria()
    {
        var engine = new RecordingSimulationEngine();
        var service = CreateService(engine, new InMemoryRunStore());

        var response = service.CreateStress(new ApiStressSweepRequest
        {
            Scenarios = ["service-scenario"],
            Seeds = [111],
            Models = ["need-based-allocation"],
            FailureCriteria =
            [
                new FailureCriterion("Needs Met", FailureOperator.LessThan, 1.5)
            ]
        });

        var collapse = Assert.Single(response.CollapseEvents);
        Assert.Equal("Needs Met", collapse.Metric);
        Assert.True(collapse.Collapsed);
        Assert.Equal(9, collapse.CollapseTick);
    }

    [Fact]
    public void CreateStressRejectsOverLimitRequest()
    {
        var service = CreateService(new RecordingSimulationEngine(), new InMemoryRunStore());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.CreateStress(new ApiStressSweepRequest
            {
                Scenarios = ["service-scenario"],
                Seeds = [1, 2],
                Models = ["need-based-allocation", "market-based-allocation"],
                Sweep = new Dictionary<string, IReadOnlyList<double>>
                {
                    ["needPriorityWeight"] = [1.0, 2.0]
                },
                MaxRuns = 7
            }));

        Assert.Equal("Stress sweep would create 8 runs; the maximum is 7.", exception.Message);
    }

    [Fact]
    public void CreateRunRejectsUnknownModel()
    {
        var service = CreateService(new RecordingSimulationEngine(), new InMemoryRunStore());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.CreateRun(new CreateRunRequest
            {
                Scenario = "service-scenario",
                Models = ["unknown-model"]
            }));

        Assert.Equal("Unknown model 'unknown-model'.", exception.Message);
    }

    [Fact]
    public void CreateRunRejectsNullRequest()
    {
        var service = CreateService(new RecordingSimulationEngine(), new InMemoryRunStore());

        Assert.Throws<ArgumentNullException>(() => service.CreateRun(null!));
    }

    private static SimulationRunService CreateService(RecordingSimulationEngine engine, IRunStore store)
    {
        var scenarioResolver = new ScenarioResolver(new StubScenarioCatalog(CreateScenario()));

        return new SimulationRunService(
            engine,
            new ModelCatalog(),
            new MetricCatalog(),
            scenarioResolver,
            store,
            new AiAnalysisService());
    }

    private static ScenarioDefinition CreateScenario()
    {
        return new ScenarioDefinition
        {
            Name = "Service Scenario",
            Seed = 123,
            Ticks = 10,
            InitialPopulation = 1,
            InitialResources = new ResourcePool
            {
                Food = 1
            }
        };
    }

    private sealed class RecordingSimulationEngine : ISimulationEngine
    {
        private readonly List<SimulationRunRequest> _requests = [];

        public IReadOnlyList<SimulationRunRequest> Requests => _requests;

        public SimulationRunRequest? LastRequest { get; private set; }

        public SimulationRunResult Run(SimulationRunRequest request)
        {
            LastRequest = request;
            _requests.Add(request);

            return new SimulationRunResult
            {
                ScenarioName = request.Scenario.Name,
                Seed = request.Seed ?? request.Scenario.Seed,
                Ticks = request.Scenario.Ticks,
                ModelResults = request.Models.Select(model => new ModelRunResult
                {
                    ModelName = model.Name,
                    ModelVersion = model.Version,
                    Metrics =
                    [
                        new MetricResult
                        {
                            Name = "Needs Met",
                            Tick = Math.Max(0, request.Scenario.Ticks - 1),
                            Value = request.Parameters.GetValueOrDefault("needPriorityWeight", 1),
                            Unit = "ratio"
                        }
                    ]
                }).ToArray()
            };
        }
    }

    private sealed class StubScenarioCatalog(ScenarioDefinition scenario) : IScenarioCatalog
    {
        public IReadOnlyList<ScenarioDefinition> All => [scenario];

        public ScenarioDefinition FindByName(string name)
        {
            return scenario;
        }
    }
}
