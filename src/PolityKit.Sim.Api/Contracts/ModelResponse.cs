namespace PolityKit.Sim.Api.Contracts;

public sealed class ModelResponse
{
    public string Name { get; init; } = "";

    public string Version { get; init; } = "";

    public string Kind { get; init; } = "baseline";

    public string? Description { get; init; }

    public IReadOnlyList<AssumptionResponse> Assumptions { get; init; } = [];

    public IReadOnlyList<GovernanceDimensionResponse> GovernanceDimensions { get; init; } = [];

    public IReadOnlyList<string> KnownFailureModes { get; init; } = [];

    public GovernancePresetResponse? Preset { get; init; }
}

public sealed class AssumptionResponse
{
    public string Name { get; init; } = "";

    public double Default { get; init; }

    public string Description { get; init; } = "";
}

public sealed class GovernancePresetResponse
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string Description { get; init; } = "";

    public IReadOnlyList<string> Assumptions { get; init; } = [];

    public IReadOnlyList<string> KnownFailureModes { get; init; } = [];
}

public sealed class GovernanceDimensionResponse
{
    public string DimensionId { get; init; } = "";

    public string DimensionName { get; init; } = "";

    public string ValueId { get; init; } = "";

    public string ValueName { get; init; } = "";

    public string Description { get; init; } = "";

    public string Assumption { get; init; } = "";

    public IReadOnlyDictionary<string, double> Parameters { get; init; } = new Dictionary<string, double>();

    public IReadOnlyList<string> KnownFailureModes { get; init; } = [];
}
