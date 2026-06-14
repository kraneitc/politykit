using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Tests.Engine;

public sealed class SimulationEngineTests
{
    [Fact]
    public void RunUsesScenarioSeedWhenRequestSeedIsNotProvided()
    {
        var worldFactory = new RecordingWorldFactory();
        var engine = new SimulationEngine(worldFactory, new RecordingWorldRule(), []);

        var result = engine.Run(new SimulationRunRequest
        {
            Scenario = new ScenarioDefinition
            {
                Name = "Seeded Scenario",
                Seed = 123,
                Ticks = 1
            },
            Models = [new RecordingModel("ModelA")]
        });

        Assert.Equal(123, result.Seed);
        Assert.Equal([123], worldFactory.Seeds);
    }

    [Fact]
    public void RunUsesRequestSeedWhenProvided()
    {
        var worldFactory = new RecordingWorldFactory();
        var engine = new SimulationEngine(worldFactory, new RecordingWorldRule(), []);

        var result = engine.Run(new SimulationRunRequest
        {
            Scenario = new ScenarioDefinition
            {
                Name = "Seeded Scenario",
                Seed = 123,
                Ticks = 1
            },
            Seed = 456,
            Models = [new RecordingModel("ModelA")]
        });

        Assert.Equal(456, result.Seed);
        Assert.Equal([456], worldFactory.Seeds);
    }

    [Fact]
    public void RunCreatesIsolatedWorldForEachModel()
    {
        var worldFactory = new RecordingWorldFactory();
        var firstModel = new RecordingModel("FirstModel");
        var secondModel = new RecordingModel("SecondModel");
        var engine = new SimulationEngine(worldFactory, new RecordingWorldRule(), []);

        var result = engine.Run(new SimulationRunRequest
        {
            Scenario = new ScenarioDefinition
            {
                Name = "Two Model Scenario",
                Seed = 99,
                Ticks = 1
            },
            Models = [firstModel, secondModel]
        });

        Assert.Equal(2, result.ModelResults.Count);
        Assert.Equal(2, worldFactory.Worlds.Count);
        Assert.NotSame(worldFactory.Worlds[0], worldFactory.Worlds[1]);
        Assert.Same(worldFactory.Worlds[0], firstModel.Worlds[0]);
        Assert.Same(worldFactory.Worlds[1], secondModel.Worlds[0]);
    }

    [Fact]
    public void RunPassesTickSeedRandomAndParametersToModel()
    {
        var model = new RecordingModel("ModelA");
        var parameters = new Dictionary<string, double>
        {
            ["scarcity"] = 0.75
        };
        var engine = new SimulationEngine(new RecordingWorldFactory(), new RecordingWorldRule(), []);

        engine.Run(new SimulationRunRequest
        {
            Scenario = new ScenarioDefinition
            {
                Name = "Context Scenario",
                Seed = 321,
                Ticks = 3
            },
            Models = [model],
            Parameters = parameters
        });

        Assert.Equal([0, 1, 2], model.Contexts.Select(context => context.Tick).ToArray());
        Assert.All(model.Contexts, context =>
        {
            Assert.Equal(321, context.Seed);
            Assert.Equal(321, context.Random.Seed);
            Assert.Same(parameters, context.Parameters);
        });
    }

    [Fact]
    public void RunRecordsMetricForEachTickUsingOnlyRecentEvents()
    {
        var metric = new RecordingMetric("recent-events");
        var shockHandler = new RecordingShockHandler("HandledShock");
        var engine = new SimulationEngine(
            new RecordingWorldFactory(),
            new RecordingWorldRule(),
            [shockHandler]);

        var result = engine.Run(new SimulationRunRequest
        {
            Scenario = new ScenarioDefinition
            {
                Name = "Metric Scenario",
                Seed = 1,
                Ticks = 3,
                Shocks =
                {
                    new ShockDefinition
                    {
                        Tick = 1,
                        Type = "HandledShock",
                        Severity = 0.5
                    }
                }
            },
            Models = [new RecordingModel("ModelA")],
            Metrics = [metric]
        });

        Assert.Equal([1, 2, 1], result.ModelResults[0].Metrics.Select(item => item.Value).ToArray());
        Assert.Equal([0, 1, 2], metric.Ticks);
        Assert.Equal(["RuleApplied"], metric.EventTypesByTick[0]);
        Assert.Equal(["HandledShock", "RuleApplied"], metric.EventTypesByTick[1]);
        Assert.Equal(["RuleApplied"], metric.EventTypesByTick[2]);
    }

    [Fact]
    public void RunAppliesScheduledShockBeforeModelDecision()
    {
        var model = new RecordingModel("ModelA");
        var shockHandler = new RecordingShockHandler("HandledShock");
        var engine = new SimulationEngine(
            new RecordingWorldFactory(),
            new RecordingWorldRule(),
            [shockHandler]);

        engine.Run(new SimulationRunRequest
        {
            Scenario = new ScenarioDefinition
            {
                Name = "Shock Scenario",
                Seed = 1,
                Ticks = 2,
                Shocks =
                {
                    new ShockDefinition
                    {
                        Tick = 1,
                        Type = "HandledShock",
                        Severity = 0.5
                    }
                }
            },
            Models = [model]
        });

        Assert.False(model.EventsVisibleAtDecision[0].Contains("HandledShock"));
        Assert.Contains("HandledShock", model.EventsVisibleAtDecision[1]);
    }

    [Fact]
    public void RunRejectsNullRequest()
    {
        var engine = new SimulationEngine(new RecordingWorldFactory(), new RecordingWorldRule(), []);

        Assert.Throws<ArgumentNullException>(() => engine.Run(null!));
    }

    private sealed class RecordingWorldFactory : IWorldFactory
    {
        public List<int> Seeds { get; } = [];

        public List<WorldState> Worlds { get; } = [];

        public WorldState CreateWorld(ScenarioDefinition scenario, IRandomSource random)
        {
            Seeds.Add(random.Seed);

            var world = new WorldState();
            Worlds.Add(world);

            return world;
        }
    }

    private sealed class RecordingWorldRule : IWorldRule
    {
        public void Apply(WorldState world, SystemDecision decision)
        {
            world.Events.Add(new SimulationEvent
            {
                Tick = world.Tick,
                Type = "RuleApplied"
            });
        }
    }

    private sealed class RecordingShockHandler(string type) : IShockHandler
    {
        public bool CanHandle(ShockDefinition shock)
        {
            return shock.Type == type;
        }

        public void Apply(WorldState world, ShockDefinition shock)
        {
            world.Events.Add(new SimulationEvent
            {
                Tick = world.Tick,
                Type = shock.Type
            });
        }
    }

    private sealed class RecordingModel(string name) : ISystemModel
    {
        public string Name { get; } = name;

        public string Version => "test";

        public List<SystemContext> Contexts { get; } = [];

        public List<WorldState> Worlds { get; } = [];

        public List<string[]> EventsVisibleAtDecision { get; } = [];

        public SystemDecision Decide(WorldState world, SystemContext context)
        {
            Worlds.Add(world);
            Contexts.Add(context);
            EventsVisibleAtDecision.Add(world.Events.Select(simulationEvent => simulationEvent.Type).ToArray());

            return new SystemDecision();
        }
    }

    private sealed class RecordingMetric(string name) : IMetric
    {
        public string Name { get; } = name;

        public List<int> Ticks { get; } = [];

        public List<string[]> EventTypesByTick { get; } = [];

        public double Calculate(WorldState world, IReadOnlyList<SimulationEvent> events)
        {
            Ticks.Add(world.Tick);
            EventTypesByTick.Add(events.Select(simulationEvent => simulationEvent.Type).ToArray());

            return events.Count;
        }
    }
}
