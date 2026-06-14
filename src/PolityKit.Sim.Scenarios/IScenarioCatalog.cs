using PolityKit.Sim.Core.Scenarios;

namespace PolityKit.Sim.Scenarios;

public interface IScenarioCatalog
{
    IReadOnlyList<ScenarioDefinition> All { get; }

    ScenarioDefinition? FindByName(string name);
}
