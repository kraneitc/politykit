namespace PolityKit.Sim.Core.Models;

public sealed class ModelManifest
{
    public string Model { get; init; } = "";

    public string Version { get; init; } = "";

    public string Description { get; init; } = "";

    public List<ModelAssumption> Assumptions { get; init; } = [];

    public List<GovernanceDimensionManifest> GovernanceDimensions { get; init; } = [];

    public List<string> KnownFailureModes { get; init; } = [];
}

public sealed class GovernanceDimensionManifest
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
