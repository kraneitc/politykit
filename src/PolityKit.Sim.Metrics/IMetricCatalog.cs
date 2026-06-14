using PolityKit.Sim.Core.Metrics;

namespace PolityKit.Sim.Metrics;

public interface IMetricCatalog
{
    IReadOnlyList<IMetric> All { get; }

    IMetric? FindByName(string name);
}
