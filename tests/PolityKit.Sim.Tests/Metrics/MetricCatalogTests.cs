using PolityKit.Sim.Metrics;

namespace PolityKit.Sim.Tests.Metrics;

public sealed class MetricCatalogTests
{
    [Fact]
    public void DefaultCatalogIncludesStarterMetrics()
    {
        var catalog = new MetricCatalog();

        var names = catalog.All.Select(metric => metric.Name).ToArray();

        Assert.Contains("Needs Met", names);
        Assert.Contains("Inequality", names);
        Assert.Contains("Trust", names);
        Assert.Contains("Severe Failures", names);
        Assert.Contains("Administrative Load", names);
    }

    [Fact]
    public void FindByNameIsCaseInsensitive()
    {
        var catalog = new MetricCatalog();

        var metric = catalog.FindByName("needs met");

        Assert.NotNull(metric);
        Assert.Equal("Needs Met", metric.Name);
    }

    [Fact]
    public void FindByNameRejectsBlankName()
    {
        var catalog = new MetricCatalog();

        Assert.Throws<ArgumentException>(() => catalog.FindByName(""));
    }
}
