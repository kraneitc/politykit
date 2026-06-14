using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Metrics;

public sealed class InequalityMetric : IMetric
{
    public string Name => "Inequality";

    public double Calculate(WorldState world, IReadOnlyList<SimulationEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);

        var values = world.Population.Citizens
            .Select(citizen => Math.Max(0, citizen.Wealth))
            .Order()
            .ToArray();

        if (values.Length == 0)
        {
            return 0.0;
        }

        var total = values.Sum();
        if (total == 0)
        {
            return 0.0;
        }

        double weightedSum = 0;
        for (var index = 0; index < values.Length; index++)
        {
            weightedSum += (index + 1) * values[index];
        }

        return (2 * weightedSum) / (values.Length * total) - ((double)values.Length + 1) / values.Length;
    }
}
