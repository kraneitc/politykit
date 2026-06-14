using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.Scenarios;

namespace PolityKit.Sim.Engine;

public sealed class SimulationRunRequest
{
    public ScenarioDefinition Scenario { get; init; } = new();

    public int? Seed { get; init; }

    public IReadOnlyList<ISystemModel> Models { get; init; } = [];

    public IReadOnlyList<IMetric> Metrics { get; init; } = [];

    public IReadOnlyDictionary<string, double> Parameters { get; init; } = new Dictionary<string, double>();
}
