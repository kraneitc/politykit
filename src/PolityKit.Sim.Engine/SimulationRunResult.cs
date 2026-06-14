namespace PolityKit.Sim.Engine;

public sealed class SimulationRunResult
{
    public string ScenarioName { get; init; } = "";

    public int Seed { get; init; }

    public int Ticks { get; init; }

    public IReadOnlyList<ModelRunResult> ModelResults { get; init; } = [];
}
