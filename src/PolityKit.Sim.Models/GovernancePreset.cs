using PolityKit.Sim.Core.Models.Governance;

namespace PolityKit.Sim.Models;

public sealed record GovernancePreset
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string Description { get; init; } = "";

    public IReadOnlyList<string> Assumptions { get; init; } = [];

    public IReadOnlyList<string> KnownFailureModes { get; init; } = [];

    public GovernanceProfile Profile { get; init; } = new();
}
