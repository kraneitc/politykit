using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Api.Contracts;

public sealed class RunDashboardResponse
{
    public Guid Id { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string ScenarioName { get; init; } = "";

    public int Seed { get; init; }

    public int Ticks { get; init; }

    public SimulationRunSummary Summary { get; init; } = new();

    public IReadOnlyList<MetricResponse> Metrics { get; init; } = [];

    public IReadOnlyList<EventResponse> Events { get; init; } = [];
}
