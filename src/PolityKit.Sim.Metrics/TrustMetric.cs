using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Metrics;

public sealed class TrustMetric : IMetric
{
    public string Name => "Trust";

    public double Calculate(WorldState world, IReadOnlyList<SimulationEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (world.Population.Count == 0)
        {
            return world.Institutions.Trust;
        }

        var citizenTrust = world.Population.Citizens.Average(citizen => citizen.TrustInSystem);

        return (citizenTrust + world.Institutions.Trust) / 2.0;
    }
}
