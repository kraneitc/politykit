using PolityKit.Sim.Core.Scenarios;

namespace PolityKit.Sim.Scenarios;

public sealed class ScenarioValidator : IScenarioValidator
{
    public ScenarioValidationResult Validate(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(scenario.Name))
        {
            errors.Add("Scenario name is required.");
        }

        if (scenario.Ticks <= 0)
        {
            errors.Add("Scenario ticks must be greater than zero.");
        }

        if (scenario.InitialPopulation < 0)
        {
            errors.Add("Initial population cannot be negative.");
        }

        ValidateResources(scenario, errors);
        ValidateShocks(scenario, errors);

        return new ScenarioValidationResult
        {
            Errors = errors
        };
    }

    private static void ValidateResources(ScenarioDefinition scenario, List<string> errors)
    {
        if (scenario.InitialResources.Food < 0)
        {
            errors.Add("Initial food cannot be negative.");
        }

        if (scenario.InitialResources.Medicine < 0)
        {
            errors.Add("Initial medicine cannot be negative.");
        }

        if (scenario.InitialResources.Housing < 0)
        {
            errors.Add("Initial housing cannot be negative.");
        }

        if (scenario.InitialResources.AdminCapacity < 0)
        {
            errors.Add("Initial admin capacity cannot be negative.");
        }

        if (scenario.InitialResources.ProductionCapacity < 0)
        {
            errors.Add("Initial production capacity cannot be negative.");
        }
    }

    private static void ValidateShocks(ScenarioDefinition scenario, List<string> errors)
    {
        foreach (var shock in scenario.Shocks)
        {
            if (shock.Tick < 0 || shock.Tick >= scenario.Ticks)
            {
                errors.Add($"Shock '{shock.Type}' has tick {shock.Tick}, which is outside the scenario range.");
            }

            if (string.IsNullOrWhiteSpace(shock.Type))
            {
                errors.Add("Shock type is required.");
            }

            if (shock.Severity is < 0 or > 1)
            {
                errors.Add($"Shock '{shock.Type}' severity must be between 0 and 1.");
            }
        }
    }
}
