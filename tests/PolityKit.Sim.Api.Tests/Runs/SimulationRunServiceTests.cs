using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Services;
using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Engine;
using PolityKit.Sim.Metrics;
using PolityKit.Sim.Models;
using PolityKit.Sim.Scenarios;

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
        Assert.Equal(["NeedBasedAllocation", "MarketBasedAllocation", "HierarchyBasedAllocation"], engine.LastRequest.Models.Select(model => model.Name).ToArray());
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
            store);
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
        public SimulationRunRequest? LastRequest { get; private set; }

        public SimulationRunResult Run(SimulationRunRequest request)
        {
            LastRequest = request;

            return new SimulationRunResult
            {
                ScenarioName = request.Scenario.Name,
                Seed = request.Seed ?? request.Scenario.Seed,
                Ticks = request.Scenario.Ticks,
                ModelResults = request.Models.Select(model => new ModelRunResult
                {
                    ModelName = model.Name,
                    ModelVersion = model.Version
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
