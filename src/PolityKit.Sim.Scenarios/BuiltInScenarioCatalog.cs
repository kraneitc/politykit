using PolityKit.Sim.Core.Scenarios;

namespace PolityKit.Sim.Scenarios;

public sealed class BuiltInScenarioCatalog : IScenarioCatalog
{
    private readonly IReadOnlyList<ScenarioDefinition> _scenarios =
    [
        BuiltInScenarios.VillageFoodCrisis()
    ];

    public IReadOnlyList<ScenarioDefinition> All => _scenarios;

    public ScenarioDefinition? FindByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _scenarios.FirstOrDefault(scenario =>
            string.Equals(scenario.Name, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ScenarioNames.ToSlug(scenario.Name), name, StringComparison.OrdinalIgnoreCase));
    }
}
