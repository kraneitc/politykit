using PolityKit.Sim.Models;

namespace PolityKit.Sim.Tests.Models;

public sealed class ModelCatalogTests
{
    [Fact]
    public void DefaultCatalogIncludesStarterAllocationModels()
    {
        var catalog = new ModelCatalog();

        var names = catalog.All.Select(model => model.Name).ToArray();

        Assert.Contains("NeedBasedAllocation", names);
        Assert.Contains("MarketBasedAllocation", names);
        Assert.Contains("HierarchyBasedAllocation", names);
    }

    [Fact]
    public void FindByNameAcceptsKebabCaseNames()
    {
        var catalog = new ModelCatalog();

        var model = catalog.FindByName("need-based-allocation");

        Assert.NotNull(model);
        Assert.Equal("NeedBasedAllocation", model.Name);
    }
}
