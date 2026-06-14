using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Metrics;

public sealed class SevereFailuresMetric : IMetric
{
    public string Name => "Severe Failures";

    public double Calculate(WorldState world, IReadOnlyList<SimulationEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);

        var severeEventCount = events.Count(IsSevereEvent);
        var citizensInSevereNeed = world.Population.Citizens.Count(citizen =>
            citizen.FoodNeed >= 3 ||
            citizen.HealthNeed >= 2 ||
            citizen.TrustInSystem <= 10);

        return severeEventCount + citizensInSevereNeed;
    }

    private static bool IsSevereEvent(SimulationEvent simEvent)
    {
        return simEvent.Type is "SevereFailure" or "AdministrativeBacklog"
            || (simEvent.Type == "UnmetNeeds" && ReadUnmetNeed(simEvent) > 0);
    }

    private static double ReadUnmetNeed(SimulationEvent simEvent)
    {
        return simEvent.Data.TryGetValue("unmetNeed", out var value) && value is IConvertible convertible
            ? convertible.ToDouble(null)
            : 0.0;
    }
}
