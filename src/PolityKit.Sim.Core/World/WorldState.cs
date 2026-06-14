using PolityKit.Sim.Core.Events;

namespace PolityKit.Sim.Core.World;

public sealed class WorldState
{
    public int Tick { get; set; }

    public Population Population { get; init; } = new();

    public ResourcePool Resources { get; init; } = new();

    public InstitutionalState Institutions { get; init; } = new();

    public EnvironmentState Environment { get; init; } = new();

    public List<SimulationEvent> Events { get; init; } = [];
}
