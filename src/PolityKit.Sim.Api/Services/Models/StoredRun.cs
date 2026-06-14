using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Api.Services.Models;

public sealed class StoredRun
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public SimulationRunResult Result { get; init; } = new();
}
