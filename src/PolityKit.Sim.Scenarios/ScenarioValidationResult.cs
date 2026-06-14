namespace PolityKit.Sim.Scenarios;

public sealed class ScenarioValidationResult
{
    public IReadOnlyList<string> Errors { get; init; } = [];

    public bool IsValid => Errors.Count == 0;
}
