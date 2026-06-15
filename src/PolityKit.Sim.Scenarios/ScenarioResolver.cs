using PolityKit.Sim.Core.Scenarios;

namespace PolityKit.Sim.Scenarios;

public sealed class ScenarioResolver(
    IScenarioCatalog? catalog = null,
    IScenarioLoader? loader = null,
    IScenarioValidator? validator = null,
    bool allowFilePaths = true)
{
    private readonly IScenarioCatalog _catalog = catalog ?? new BuiltInScenarioCatalog();
    private readonly IScenarioLoader _loader = loader ?? new JsonScenarioLoader(validator);
    private readonly IScenarioValidator _validator = validator ?? new ScenarioValidator();

    public ScenarioDefinition Resolve(string? nameOrPath)
    {
        var scenario = string.IsNullOrWhiteSpace(nameOrPath)
            ? _catalog.FindByName("village-food-crisis")
            : _catalog.FindByName(nameOrPath) ?? LoadByPath(nameOrPath);

        if (scenario is null)
        {
            throw new InvalidOperationException($"Scenario '{nameOrPath}' was not found.");
        }

        var validation = _validator.Validate(scenario);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Scenario '{scenario.Name}' is invalid: {string.Join("; ", validation.Errors)}");
        }

        return ScenarioDefinitionExtensions.Clone(scenario);
    }

    private ScenarioDefinition? LoadByPath(string nameOrPath)
    {
        return allowFilePaths && File.Exists(nameOrPath)
            ? _loader.Load(nameOrPath)
            : null;
    }
}
