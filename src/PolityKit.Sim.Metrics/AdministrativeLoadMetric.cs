using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Metrics;

public sealed class AdministrativeLoadMetric : IMetric
{
    public string Name => "Administrative Load";

    public double Calculate(WorldState world, IReadOnlyList<SimulationEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        var backlogOverflow = events
            .Where(simEvent => simEvent.Type == "AdministrativeBacklog")
            .Sum(ReadOverflow);

        return world.Institutions.AppealBacklog + backlogOverflow;
    }

    private static double ReadOverflow(SimulationEvent simEvent)
    {
        return simEvent.Data.TryGetValue("overflow", out var value) && value is IConvertible convertible
            ? convertible.ToDouble(null)
            : 0.0;
    }
}
