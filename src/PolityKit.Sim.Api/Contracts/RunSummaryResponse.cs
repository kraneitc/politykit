namespace PolityKit.Sim.Api.Contracts;

public sealed class RunSummaryResponse
{
    public Guid Id { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string ScenarioName { get; init; } = "";

    public int Seed { get; init; }

    public int Ticks { get; init; }

    public IReadOnlyList<string> Models { get; init; } = [];
}

public sealed class ModelRunSummaryResponse
{
    public string ModelName { get; init; } = "";

    public string ModelVersion { get; init; } = "";

    public int EventCount { get; init; }

    public IReadOnlyList<MetricResponse> FinalMetrics { get; init; } = [];
}
