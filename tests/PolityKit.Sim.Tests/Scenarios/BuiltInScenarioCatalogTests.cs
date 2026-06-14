using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Tests.Scenarios;

public sealed class BuiltInScenarioCatalogTests
{
    [Fact]
    public void DefaultCatalogIncludesVillageFoodCrisis()
    {
        var catalog = new BuiltInScenarioCatalog();

        var scenario = catalog.FindByName("village-food-crisis");

        Assert.NotNull(scenario);
        Assert.Equal("Village Food Crisis", scenario.Name);
    }
}
