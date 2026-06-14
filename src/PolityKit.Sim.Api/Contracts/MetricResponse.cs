namespace PolityKit.Sim.Api.Contracts;

public sealed class MetricResponse
{
    public string Model { get; init; } = "";

    public int Tick { get; init; }

    public string Name { get; init; } = "";

    public double Value { get; init; }

    public string Unit { get; init; } = "";
}
