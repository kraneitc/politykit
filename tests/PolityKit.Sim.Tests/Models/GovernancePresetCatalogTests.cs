using PolityKit.Sim.Core.Models.Governance;
using PolityKit.Sim.Engine;
using PolityKit.Sim.Metrics;
using PolityKit.Sim.Models;
using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Tests.Models;

public sealed class GovernancePresetCatalogTests
{
    [Fact]
    public void DefaultCatalogIncludesStarterPresetsWithStableIds()
    {
        var catalog = new GovernancePresetCatalog();

        var ids = catalog.All.Select(preset => preset.Id).ToArray();

        Assert.Equal(
            [
                "participatory-commons",
                "regulated-market",
                "central-planning",
                "patronage-hierarchy",
                "mutual-aid-federation",
                "technocratic-administration"
            ],
            ids);
    }

    [Fact]
    public void PresetsExposeMetadataAndValidProfiles()
    {
        var catalog = new GovernancePresetCatalog();

        foreach (var preset in catalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(preset.Id));
            Assert.False(string.IsNullOrWhiteSpace(preset.Name));
            Assert.False(string.IsNullOrWhiteSpace(preset.Description));
            Assert.NotEmpty(preset.Assumptions);
            Assert.NotEmpty(preset.KnownFailureModes);
            Assert.Equal(preset.Id, preset.Profile.Id);
            Assert.Equal(preset.Name, preset.Profile.Name);
            Assert.Equal(6, preset.Profile.Dimensions().Count);

            var validation = GovernanceProfileValidator.Validate(preset.Profile);
            Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
        }
    }

    [Fact]
    public void FindByIdAcceptsCaseInsensitiveStableId()
    {
        var catalog = new GovernancePresetCatalog();

        var preset = catalog.FindById("REGULATED-MARKET");

        Assert.NotNull(preset);
        Assert.Equal("Regulated Market", preset.Name);
    }

    [Fact]
    public void FindByNameAcceptsExactDisplayName()
    {
        var catalog = new GovernancePresetCatalog();

        var preset = catalog.FindByName("Participatory Commons");

        Assert.NotNull(preset);
        Assert.Equal("participatory-commons", preset.Id);
    }

    [Fact]
    public void FindByNameAcceptsKebabCaseDisplayName()
    {
        var catalog = new GovernancePresetCatalog();

        var preset = catalog.FindByName("mutual-aid-federation");

        Assert.NotNull(preset);
        Assert.Equal("Mutual Aid Federation", preset.Name);
    }

    [Fact]
    public void FindAcceptsIdOrName()
    {
        var catalog = new GovernancePresetCatalog();

        Assert.Equal("central-planning", catalog.Find("Central Planning")?.Id);
        Assert.Equal("Patronage Hierarchy", catalog.Find("patronage-hierarchy")?.Name);
    }

    [Fact]
    public void LookupRejectsBlankValues()
    {
        var catalog = new GovernancePresetCatalog();

        Assert.Throws<ArgumentException>(() => catalog.FindById(""));
        Assert.Throws<ArgumentException>(() => catalog.FindByName(""));
        Assert.Throws<ArgumentException>(() => catalog.Find(""));
    }

    [Fact]
    public void EveryPresetCanRunAgainstBuiltInScenario()
    {
        var presetCatalog = new GovernancePresetCatalog();
        var scenario = new BuiltInScenarioCatalog().FindByName("village-food-crisis")!;
        var engine = new SimulationEngine();

        var result = engine.Run(new SimulationRunRequest
        {
            Scenario = scenario,
            Models = presetCatalog.All
                .Select(preset => new CompositeGovernanceModel(preset.Profile))
                .ToArray(),
            Metrics = DefaultMetricSet.Create()
        });

        Assert.Equal(presetCatalog.All.Count, result.ModelResults.Count);
        foreach (var preset in presetCatalog.All)
        {
            var modelResult = Assert.Single(result.ModelResults, candidate =>
                candidate.ModelName == $"CompositeGovernance:{preset.Id}");

            Assert.NotEmpty(modelResult.Metrics);
            Assert.NotEmpty(modelResult.Events);
        }
    }
}
