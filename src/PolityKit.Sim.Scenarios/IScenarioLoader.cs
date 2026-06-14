using PolityKit.Sim.Core.Scenarios;

namespace PolityKit.Sim.Scenarios;

public interface IScenarioLoader
{
    ScenarioDefinition Load(string path);
}
