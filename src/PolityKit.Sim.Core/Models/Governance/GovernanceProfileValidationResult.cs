namespace PolityKit.Sim.Core.Models.Governance;

public sealed class GovernanceProfileValidationResult
{
    public IReadOnlyList<string> Errors { get; init; } = [];

    public bool IsValid => Errors.Count == 0;
}
