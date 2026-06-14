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
            citizen.FoodNeed == 0 &&
            citizen.HealthNeed == 0 &&
            citizen.HousingNeed == 0);

        return (double)citizensWithNeedsMet / world.Population.Count;
    }
}
