namespace PolityKit.Sim.Core.Models;

public sealed class PolicyChange
{
    public string Name { get; init; } = "";

    public double Value { get; init; }

    public string Reason { get; init; } = "";
}
