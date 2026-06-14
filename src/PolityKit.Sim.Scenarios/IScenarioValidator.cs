using PolityKit.Sim.Core.Scenarios;

namespace PolityKit.Sim.Scenarios;

public interface IScenarioValidator
{
    ScenarioValidationResult Validate(ScenarioDefinition scenario);
}
