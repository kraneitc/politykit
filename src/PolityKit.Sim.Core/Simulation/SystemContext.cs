namespace PolityKit.Sim.Core.Simulation;

public sealed class SystemContext
{
    public int Tick { get; init; }

    public int Seed { get; init; }

    public IRandomSource Random { get; init; } = new SeededRandomSource(0);

    public IReadOnlyDictionary<string, double> Parameters { get; init; } = new Dictionary<string, double>();
}
