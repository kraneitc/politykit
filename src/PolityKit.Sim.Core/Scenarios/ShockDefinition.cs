namespace PolityKit.Sim.Core.Scenarios;

public sealed class ShockDefinition
{
    public int Tick { get; init; }

    public string Type { get; init; } = "";

    public double Severity { get; init; }

    public Dictionary<string, object> Parameters { get; init; } = [];
}
