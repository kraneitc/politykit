namespace PolityKit.Sim.Core.Models;

public sealed class ModelManifest
{
    public string Model { get; init; } = "";

    public string Version { get; init; } = "";

    public string Description { get; init; } = "";

    public List<ModelAssumption> Assumptions { get; init; } = [];

    public List<string> KnownFailureModes { get; init; } = [];
}
