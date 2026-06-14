using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Tests.Scenarios;

public sealed class BuiltInScenariosTests
{
    [Fact]
    public void VillageFoodCrisisHasExpectedBaselineShape()
    {
        var scenario = BuiltInScenarios.VillageFoodCrisis();

        Assert.Equal("Village Food Crisis", scenario.Name);
        Assert.Equal(12345, scenario.Seed);
        Assert.Equal(120, scenario.Ticks);
        Assert.Equal(500, scenario.InitialPopulation);
        Assert.Equal(800, scenario.InitialResources.Food);
        Assert.Equal(120, scenario.InitialResources.Medicine);
        Assert.Equal(450, scenario.InitialResources.Housing);
        Assert.Equal(80, scenario.InitialResources.AdminCapacity);
        Assert.Equal(100, scenario.InitialResources.ProductionCapacity);
        Assert.Collection(
            scenario.Shocks,
            shock =>
            {
                Assert.Equal(20, shock.Tick);
                Assert.Equal("CropFailure", shock.Type);
                Assert.Equal(0.4, shock.Severity);
            },
            shock =>
            {
                Assert.Equal(45, shock.Tick);
                Assert.Equal("AdministrativeOverload", shock.Type);
                Assert.Equal(0.3, shock.Severity);
            });
    }
}
