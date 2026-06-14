namespace PolityKit.Sim.Core.Events;

public sealed class SimulationEvent
{
    public int Tick { get; init; }

    public string Type { get; init; } = "";

    public string Description { get; init; } = "";

    public Dictionary<string, object> Data { get; init; } = [];
}
