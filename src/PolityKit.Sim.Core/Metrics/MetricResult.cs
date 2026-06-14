namespace PolityKit.Sim.Core.Metrics;

public sealed class MetricResult
{
    public string Name { get; init; } = "";

    public int Tick { get; init; }

    public double Value { get; init; }

    public string Unit { get; init; } = "";
}
