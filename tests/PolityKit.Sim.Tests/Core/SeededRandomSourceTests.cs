using PolityKit.Sim.Core.Simulation;

namespace PolityKit.Sim.Tests.Core;

public sealed class SeededRandomSourceTests
{
    [Fact]
    public void SameSeedProducesSameIntegerSequence()
    {
        var first = new SeededRandomSource(12345);
        var second = new SeededRandomSource(12345);

        var firstValues = Enumerable.Range(0, 10)
            .Select(_ => first.Next(0, 100))
            .ToArray();
        var secondValues = Enumerable.Range(0, 10)
            .Select(_ => second.Next(0, 100))
            .ToArray();

        Assert.Equal(firstValues, secondValues);
    }

    [Fact]
    public void SameSeedProducesSameDoubleSequence()
    {
        var first = new SeededRandomSource(6789);
        var second = new SeededRandomSource(6789);

        var firstValues = Enumerable.Range(0, 10)
            .Select(_ => first.NextDouble())
            .ToArray();
        var secondValues = Enumerable.Range(0, 10)
            .Select(_ => second.NextDouble())
            .ToArray();

        Assert.Equal(firstValues, secondValues);
    }

    [Fact]
    public void ExposesOriginalSeed()
    {
        var random = new SeededRandomSource(42);

        Assert.Equal(42, random.Seed);
    }
}
