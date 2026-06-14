using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Tests.Scenarios;

public sealed class ScenarioNamesTests
{
    [Theory]
    [InlineData("Village Food Crisis", "village-food-crisis")]
    [InlineData("  Village: Food  Crisis!  ", "village-food-crisis")]
    [InlineData("Admin_Overload/Crop Failure", "admin-overload-crop-failure")]
    [InlineData("Scenario 42", "scenario-42")]
    public void ToSlugNormalizesScenarioNames(string value, string expected)
    {
        var slug = ScenarioNames.ToSlug(value);

        Assert.Equal(expected, slug);
    }

    [Fact]
    public void ToSlugRejectsBlankNames()
    {
        Assert.Throws<ArgumentException>(() => ScenarioNames.ToSlug(""));
    }
}
