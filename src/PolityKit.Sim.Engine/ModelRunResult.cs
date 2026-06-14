using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Engine;

public sealed class ModelRunResult
{
    public string ModelName { get; init; } = "";

    public string ModelVersion { get; init; } = "";

    public WorldState World { get; init; } = new();

    public IReadOnlyList<SimulationEvent> Events { get; init; } = [];

    public IReadOnlyList<MetricResult> Metrics { get; init; } = [];
}
