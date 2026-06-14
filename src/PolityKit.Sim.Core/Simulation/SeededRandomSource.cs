namespace PolityKit.Sim.Core.Simulation;

public sealed class SeededRandomSource(int seed) : IRandomSource
{
    private readonly Random _random = new(seed);

    public int Seed { get; } = seed;

    public int Next(int minValue, int maxValue)
    {
        return _random.Next(minValue, maxValue);
    }

    public double NextDouble()
    {
        return _random.NextDouble();
    }
}
