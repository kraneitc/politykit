using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Metrics;

public sealed class NeedsMetMetric : IMetric
{
    public string Name => "Needs Met";

    public double Calculate(WorldState world, IReadOnlyList<SimulationEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Population.Count == 0)
        {
            return 1.0;
        }

        var citizensWithNeedsMet = world.Population.Citizens.Count(citizen =>
            citizen is { FoodNeed: 0, HealthNeed: 0, HousingNeed: 0 });

        return (double)citizensWithNeedsMet / world.Population.Count;
    }
}
