using PolityKit.Sim.Engine;
using PolityKit.Sim.Core.Simulation;

namespace PolityKit.Sim.Api.Services.Models;

public sealed class StoredRun
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public RunConfiguration Configuration { get; init; } = new();

    public SimulationRunResult Result { get; init; } = new();
}
