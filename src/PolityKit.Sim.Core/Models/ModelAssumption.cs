namespace PolityKit.Sim.Core.Models;

public sealed class ModelAssumption
{
    public string Name { get; init; } = "";

    public double Default { get; init; }

    public string Description { get; init; } = "";
}
