using PolityKit.Sim.Core.Metrics;

namespace PolityKit.Sim.Metrics;

public static class DefaultMetricSet
{
    public static IReadOnlyList<IMetric> Create()
    {
        return
        [
            new NeedsMetMetric(),
            new InequalityMetric(),
            new TrustMetric(),
            new SevereFailuresMetric(),
            new AdministrativeLoadMetric()
        ];
    }
}
