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
    public void DefaultCatalogIncludesGovernancePresetModels()
    {
        var catalog = new ModelCatalog();

        var names = catalog.All.Select(model => model.Name).ToArray();

        Assert.Contains("CompositeGovernance:participatory-commons", names);
        Assert.Contains("CompositeGovernance:regulated-market", names);
        Assert.Contains("CompositeGovernance:central-planning", names);
        Assert.Contains("CompositeGovernance:patronage-hierarchy", names);
        Assert.Contains("CompositeGovernance:mutual-aid-federation", names);
        Assert.Contains("CompositeGovernance:technocratic-administration", names);
    }

    [Fact]
    public void FindByNameAcceptsKebabCaseNames()
    {
        var catalog = new ModelCatalog();

        var model = catalog.FindByName("need-based-allocation");

        Assert.NotNull(model);
        Assert.Equal("NeedBasedAllocation", model.Name);
    }

    [Fact]
    public void FindByNameAcceptsCaseInsensitiveModelNames()
    {
        var catalog = new ModelCatalog();

        var model = catalog.FindByName("marketbasedallocation");

        Assert.NotNull(model);
        Assert.Equal("MarketBasedAllocation", model.Name);
    }

    [Theory]
    [InlineData("regulated-market")]
    [InlineData("preset:regulated-market")]
    [InlineData("Regulated Market")]
    [InlineData("CompositeGovernance:regulated-market")]
    public void FindByNameAcceptsGovernancePresetSelectors(string selector)
    {
        var catalog = new ModelCatalog();

        var model = catalog.FindByName(selector);

        Assert.NotNull(model);
        Assert.Equal("CompositeGovernance:regulated-market", model.Name);
        var compositeModel = Assert.IsType<CompositeGovernanceModel>(model);
        Assert.Equal("regulated-market", compositeModel.Profile.Id);
    }

    [Fact]
    public void FindByNameRejectsBlankName()
    {
        var catalog = new ModelCatalog();

        Assert.Throws<ArgumentException>(() => catalog.FindByName(""));
    }
}
