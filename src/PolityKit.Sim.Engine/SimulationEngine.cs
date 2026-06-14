using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Engine;

public sealed class SimulationEngine(
    IWorldFactory worldFactory,
    IWorldRule worldRule,
    IReadOnlyList<IShockHandler> shockHandlers)
    : ISimulationEngine
{
    public SimulationEngine()
        : this(new DefaultWorldFactory(), new DefaultWorldRule(), [new DefaultShockHandler()])
    {
    }

    public SimulationRunResult Run(SimulationRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Scenario);

        var seed = request.Seed ?? request.Scenario.Seed;
        var modelResults = new List<ModelRunResult>();

        foreach (var model in request.Models)
        {
            var random = new SeededRandomSource(seed);
            var world = worldFactory.CreateWorld(request.Scenario, random);
            var metrics = RunModel(request, model, random, world);

            modelResults.Add(new ModelRunResult
            {
                ModelName = model.Name,
                ModelVersion = model.Version,
                World = world,
                Events = [.. world.Events],
                Metrics = metrics
            });
        }

        return new SimulationRunResult
        {
            ScenarioName = request.Scenario.Name,
            Seed = seed,
            Ticks = request.Scenario.Ticks,
            ModelResults = modelResults
        };
    }

    private IReadOnlyList<MetricResult> RunModel(
        SimulationRunRequest request,
        Core.Models.ISystemModel model,
        IRandomSource random,
        WorldState world)
    {
        var metricResults = new List<MetricResult>();
        var eventCursor = 0;

        for (var tick = 0; tick < request.Scenario.Ticks; tick++)
        {
            world.Tick = tick;
            ApplyScheduledShocks(world, request.Scenario, tick);

            var context = new SystemContext
            {
                Tick = tick,
                Seed = random.Seed,
                Random = random,
                Parameters = request.Parameters
            };

            var decision = model.Decide(world, context);
            worldRule.Apply(world, decision);

            RecordMetrics(request, world, tick, eventCursor, metricResults);
            eventCursor = world.Events.Count;
        }

        return metricResults;
    }

    private void ApplyScheduledShocks(WorldState world, ScenarioDefinition scenario, int tick)
    {
        foreach (var shock in scenario.Shocks.Where(shock => shock.Tick == tick))
        {
            var handler = shockHandlers.FirstOrDefault(candidate => candidate.CanHandle(shock));
            handler?.Apply(world, shock);
        }
    }

    private static void RecordMetrics(
        SimulationRunRequest request,
        WorldState world,
        int tick,
        int eventCursor,
        List<MetricResult> metricResults)
    {
        var recentEvents = world.Events.Skip(eventCursor).ToList();

        foreach (var metric in request.Metrics)
        {
            metricResults.Add(new MetricResult
            {
                Name = metric.Name,
                Tick = tick,
                Value = metric.Calculate(world, recentEvents)
            });
        }
    }
}
