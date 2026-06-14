using PolityKit.Sim.Core.Scenarios;

namespace PolityKit.Sim.Scenarios;

public sealed class ScenarioResolver(
    IScenarioCatalog? catalog = null,
    IScenarioLoader? loader = null,
    IScenarioValidator? validator = null)
{
    private readonly IScenarioCatalog catalog = catalog ?? new BuiltInScenarioCatalog();
    private readonly IScenarioLoader loader = loader ?? new JsonScenarioLoader(validator);
    private readonly IScenarioValidator validator = validator ?? new ScenarioValidator();

    public ScenarioDefinition Resolve(string? nameOrPath)
    {
        var scenario = string.IsNullOrWhiteSpace(nameOrPath)
            ? catalog.FindByName("village-food-crisis")
            : catalog.FindByName(nameOrPath) ?? LoadByPath(nameOrPath);

        if (scenario is null)
        {
            throw new InvalidOperationException($"Scenario '{nameOrPath}' was not found.");
        }

        var validation = validator.Validate(scenario);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Scenario '{scenario.Name}' is invalid: {string.Join("; ", validation.Errors)}");
        }

        return ScenarioDefinitionExtensions.Clone(scenario);
    }

    private ScenarioDefinition? LoadByPath(string nameOrPath)
    {
        return File.Exists(nameOrPath)
            ? loader.Load(nameOrPath)
            : null;
    }
}
