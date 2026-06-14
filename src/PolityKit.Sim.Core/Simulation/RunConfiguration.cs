namespace PolityKit.Sim.Core.Simulation;

public sealed class RunConfiguration
{
    public string ScenarioName { get; init; } = "";

    public int Seed { get; init; }

    public int Ticks { get; init; }

    public List<string> ModelNames { get; init; } = [];

    public Dictionary<string, double> Parameters { get; init; } = [];
}
