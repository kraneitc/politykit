using Charterfall.App.Services;

namespace Charterfall.App.Tests;

public sealed class PrototypeContentProviderTests
{
    [Fact]
    public void GetInitialContent_ReturnsDraftShellDefaults()
    {
        var content = new PrototypeContentProvider().GetInitialContent();

        Assert.Equal("greywater-compact", content.Settlement.Id);
        Assert.Equal("Greywater Compact", content.Settlement.Name);
        Assert.Equal("failed-harvest", content.Crisis.Id);
        Assert.Equal("village-food-crisis", content.Crisis.ScenarioId);
        Assert.Equal(12345, content.Crisis.Seed);
        Assert.Equal(120, content.Crisis.Ticks);
        Assert.Contains(content.Clauses, clause => clause.Id == "allocation.need_based");
        Assert.Collection(
            content.Metrics,
            metric => Assert.Equal("Needs Met", metric.Name),
            metric => Assert.Equal("Severe Failures", metric.Name),
            metric => Assert.Equal("Trust", metric.Name),
            metric => Assert.Equal("Inequality", metric.Name),
            metric => Assert.Equal("Administrative Load", metric.Name));
        Assert.Contains("fictional institutional rules", content.ClaimsBoundary);
    }
}
