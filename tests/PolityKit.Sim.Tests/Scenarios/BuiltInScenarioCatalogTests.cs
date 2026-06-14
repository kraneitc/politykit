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

    [Fact]
    public void FindByNameAcceptsCaseInsensitiveDisplayName()
    {
        var catalog = new BuiltInScenarioCatalog();

        var scenario = catalog.FindByName("village food crisis");

        Assert.NotNull(scenario);
        Assert.Equal("Village Food Crisis", scenario.Name);
    }

    [Fact]
    public void FindByNameRejectsBlankName()
    {
        var catalog = new BuiltInScenarioCatalog();

        Assert.Throws<ArgumentException>(() => catalog.FindByName(""));
    }
}
