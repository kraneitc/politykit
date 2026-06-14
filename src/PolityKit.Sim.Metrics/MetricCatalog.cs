using PolityKit.Sim.Core.Metrics;

namespace PolityKit.Sim.Metrics;

public sealed class MetricCatalog(IEnumerable<IMetric> metrics) : IMetricCatalog
{
    private readonly IReadOnlyList<IMetric> metrics = metrics.ToArray();

    public MetricCatalog()
        : this(DefaultMetricSet.Create())
    {
    }

    public IReadOnlyList<IMetric> All => metrics;

    public IMetric? FindByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return metrics.FirstOrDefault(metric => string.Equals(metric.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
