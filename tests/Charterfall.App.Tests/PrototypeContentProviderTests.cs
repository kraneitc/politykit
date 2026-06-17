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
        Assert.Equal("village-food-crisis", content.Crisis.PolityKitScenario);
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

    [Fact]
    public void GetCampaignCrises_ReturnsThreeChaptersInOrder()
    {
        var crises = new PrototypeContentProvider().GetCampaignCrises();

        Assert.Collection(
            crises,
            crisis =>
            {
                Assert.Equal(1, crisis.ChapterNumber);
                Assert.Equal("failed-harvest", crisis.Id);
                Assert.Equal("Failed Harvest", crisis.DisplayName);
                Assert.Equal("village-food-crisis", crisis.PolityKitScenario);
                Assert.Equal(12345, crisis.Seed);
                Assert.Equal(120, crisis.Ticks);
                Assert.True(crisis.IsUnlocked);
                Assert.True(crisis.IsIntegrationAvailable);
            },
            crisis =>
            {
                Assert.Equal(2, crisis.ChapterNumber);
                Assert.Equal("fever-season", crisis.Id);
                Assert.Equal("examples/medicine-shortage.json", crisis.PolityKitScenario);
                Assert.Equal(24680, crisis.Seed);
                Assert.Equal(90, crisis.Ticks);
                Assert.False(crisis.IsUnlocked);
                Assert.False(crisis.IsIntegrationAvailable);
            },
            crisis =>
            {
                Assert.Equal(3, crisis.ChapterNumber);
                Assert.Equal("supply-office-scandal", crisis.Id);
                Assert.Equal("examples/corruption-stress.json", crisis.PolityKitScenario);
                Assert.Equal(98765, crisis.Seed);
                Assert.Equal(80, crisis.Ticks);
                Assert.False(crisis.IsUnlocked);
                Assert.False(crisis.IsIntegrationAvailable);
            });
    }

    [Fact]
    public void GetActiveCrisis_ReturnsFailedHarvestForChapterOne()
    {
        var crisis = new PrototypeContentProvider().GetActiveCrisis(1);

        Assert.Equal("failed-harvest", crisis.Id);
        Assert.Equal("Failed Harvest", crisis.DisplayName);
    }
}
