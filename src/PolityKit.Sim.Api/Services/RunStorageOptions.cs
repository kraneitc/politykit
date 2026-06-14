namespace PolityKit.Sim.Api.Services;

public sealed class RunStorageOptions
{
    public string Directory { get; init; } = Path.Combine("data", "runs");
}
