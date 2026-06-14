using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Core.Metrics;

public interface IMetric
{
    string Name { get; }

    double Calculate(WorldState world, IReadOnlyList<SimulationEvent> events);
}
