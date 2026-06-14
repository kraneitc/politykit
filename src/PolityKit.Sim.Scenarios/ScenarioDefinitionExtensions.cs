using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Scenarios;

public static class ScenarioDefinitionExtensions
{
    public static ScenarioDefinition WithSeed(this ScenarioDefinition scenario, int seed)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        return Clone(scenario, seed: seed);
    }

    public static ScenarioDefinition WithTicks(this ScenarioDefinition scenario, int ticks)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        return Clone(scenario, ticks: ticks);
    }

    public static ScenarioDefinition Clone(ScenarioDefinition scenario, int? seed = null, int? ticks = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        return new ScenarioDefinition
        {
            Name = scenario.Name,
            Seed = seed ?? scenario.Seed,
            Ticks = ticks ?? scenario.Ticks,
            InitialPopulation = scenario.InitialPopulation,
            InitialResources = Clone(scenario.InitialResources),
            Shocks = scenario.Shocks.Select(Clone).ToList()
        };
    }

    private static ResourcePool Clone(ResourcePool resources)
    {
        return new ResourcePool
        {
            Food = resources.Food,
            Medicine = resources.Medicine,
            Housing = resources.Housing,
            AdminCapacity = resources.AdminCapacity,
            ProductionCapacity = resources.ProductionCapacity
        };
    }

    private static ShockDefinition Clone(ShockDefinition shock)
    {
        return new ShockDefinition
        {
            Tick = shock.Tick,
            Type = shock.Type,
            Severity = shock.Severity,
            Parameters = new Dictionary<string, object>(shock.Parameters)
        };
    }
}
