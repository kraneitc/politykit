using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Tests.Scenarios;

public sealed class ScenarioValidatorTests
{
    [Fact]
    public void ValidateAcceptsValidScenario()
    {
        var validator = new ScenarioValidator();

        var result = validator.Validate(CreateValidScenario());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateReportsRequiredNameTicksAndPopulationRules()
    {
        var scenario = CreateValidScenario();
        scenario = new ScenarioDefinition
        {
            Name = "",
            Ticks = 0,
            InitialPopulation = -1,
            InitialResources = scenario.InitialResources
        };
        var validator = new ScenarioValidator();

        var result = validator.Validate(scenario);

        Assert.False(result.IsValid);
        Assert.Contains("Scenario name is required.", result.Errors);
        Assert.Contains("Scenario ticks must be greater than zero.", result.Errors);
        Assert.Contains("Initial population cannot be negative.", result.Errors);
    }

    [Fact]
    public void ValidateReportsNegativeInitialResources()
    {
        var scenario = new ScenarioDefinition
        {
            Name = "Invalid Resources",
            Ticks = 1,
            InitialResources = new ResourcePool
            {
                Food = -1,
                Medicine = -1,
                Housing = -1,
                AdminCapacity = -1,
                ProductionCapacity = -1
            }
        };
        var validator = new ScenarioValidator();

        var result = validator.Validate(scenario);

        Assert.Contains("Initial food cannot be negative.", result.Errors);
        Assert.Contains("Initial medicine cannot be negative.", result.Errors);
        Assert.Contains("Initial housing cannot be negative.", result.Errors);
        Assert.Contains("Initial admin capacity cannot be negative.", result.Errors);
        Assert.Contains("Initial production capacity cannot be negative.", result.Errors);
    }

    [Fact]
    public void ValidateReportsInvalidShocks()
    {
        var scenario = new ScenarioDefinition
        {
            Name = "Invalid Shocks",
            Ticks = 3,
            Shocks =
            {
                new ShockDefinition
                {
                    Tick = -1,
                    Type = "",
                    Severity = -0.1
                },
                new ShockDefinition
                {
                    Tick = 3,
                    Type = "CropFailure",
                    Severity = 1.1
                }
            }
        };
        var validator = new ScenarioValidator();

        var result = validator.Validate(scenario);

        Assert.Contains("Shock '' has tick -1, which is outside the scenario range.", result.Errors);
        Assert.Contains("Shock type is required.", result.Errors);
        Assert.Contains("Shock '' severity must be between 0 and 1.", result.Errors);
        Assert.Contains("Shock 'CropFailure' has tick 3, which is outside the scenario range.", result.Errors);
        Assert.Contains("Shock 'CropFailure' severity must be between 0 and 1.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsNullScenario()
    {
        var validator = new ScenarioValidator();

        Assert.Throws<ArgumentNullException>(() => validator.Validate(null!));
    }

    private static ScenarioDefinition CreateValidScenario()
    {
        return new ScenarioDefinition
        {
            Name = "Valid Scenario",
            Ticks = 10,
            InitialPopulation = 5,
            InitialResources = new ResourcePool
            {
                Food = 1,
                Medicine = 2,
                Housing = 3,
                AdminCapacity = 4,
                ProductionCapacity = 5
            },
            Shocks =
            {
                new ShockDefinition
                {
                    Tick = 9,
                    Type = "CropFailure",
                    Severity = 1
                }
            }
        };
    }
}
