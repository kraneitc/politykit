using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Core.Scenarios;

public sealed class ScenarioDefinition
{
    public string Name { get; init; } = "";

    public int Seed { get; init; }

    public int Ticks { get; init; }

    public int InitialPopulation { get; init; }

    public ResourcePool InitialResources { get; init; } = new();

    public List<ShockDefinition> Shocks { get; init; } = [];
}
