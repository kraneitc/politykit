namespace PolityKit.Sim.Api.Contracts;

public sealed class ModelResponse
{
    public string Name { get; init; } = "";

    public string Version { get; init; } = "";

    public string? Description { get; init; }

    public IReadOnlyList<AssumptionResponse> Assumptions { get; init; } = [];

    public IReadOnlyList<string> KnownFailureModes { get; init; } = [];
}

public sealed class AssumptionResponse
{
    public string Name { get; init; } = "";

    public double Default { get; init; }

    public string Description { get; init; } = "";
}
