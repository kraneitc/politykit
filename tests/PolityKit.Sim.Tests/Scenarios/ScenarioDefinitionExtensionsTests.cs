using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Tests.Scenarios;

public sealed class ScenarioDefinitionExtensionsTests
{
    [Fact]
    public void CloneCopiesMutableNestedState()
    {
        var original = CreateScenario();

        var clone = ScenarioDefinitionExtensions.Clone(original);

        Assert.NotSame(original, clone);
        Assert.NotSame(original.InitialResources, clone.InitialResources);
        Assert.NotSame(original.Shocks, clone.Shocks);
        Assert.NotSame(original.Shocks[0], clone.Shocks[0]);
        Assert.NotSame(original.Shocks[0].Parameters, clone.Shocks[0].Parameters);
        Assert.Equal(original.InitialResources.Food, clone.InitialResources.Food);
        Assert.Equal(original.Shocks[0].Parameters["amount"], clone.Shocks[0].Parameters["amount"]);
    }

    [Fact]
    public void CloneCanOverrideSeedAndTicks()
    {
        var original = CreateScenario();

        var clone = ScenarioDefinitionExtensions.Clone(original, seed: 42, ticks: 99);

        Assert.Equal(42, clone.Seed);
        Assert.Equal(99, clone.Ticks);
        Assert.Equal(original.Name, clone.Name);
    }

    [Fact]
    public void WithSeedAndWithTicksReturnClones()
    {
        var original = CreateScenario();

        var seeded = original.WithSeed(42);
        var reticked = original.WithTicks(99);

        Assert.Equal(42, seeded.Seed);
        Assert.Equal(original.Ticks, seeded.Ticks);
        Assert.Equal(original.Seed, reticked.Seed);
        Assert.Equal(99, reticked.Ticks);
        Assert.NotSame(original, seeded);
        Assert.NotSame(original, reticked);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithTicksRejectsNonPositiveTicks(int ticks)
    {
        var original = CreateScenario();

        var exception = Assert.Throws<InvalidOperationException>(() => original.WithTicks(ticks));

        Assert.Equal("Scenario ticks must be greater than zero.", exception.Message);
    }

    [Fact]
    public void CloneRejectsNullScenario()
    {
        Assert.Throws<ArgumentNullException>(() => ScenarioDefinitionExtensions.Clone(null!));
    }

    private static ScenarioDefinition CreateScenario()
    {
        return new ScenarioDefinition
        {
            Name = "Original",
            Seed = 1,
            Ticks = 10,
            InitialPopulation = 5,
            InitialResources = new ResourcePool
            {
                Food = 10,
                Medicine = 20,
                Housing = 30,
                AdminCapacity = 40,
                ProductionCapacity = 50
            },
            Shocks =
            {
                new ShockDefinition
                {
                    Tick = 2,
                    Type = "CropFailure",
                    Severity = 0.5,
                    Parameters = { ["amount"] = 3 }
                }
            }
        };
    }
}
