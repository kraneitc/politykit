namespace PolityKit.Sim.Api.Contracts;

public sealed class StressSweepResponse
{
    public string? GridName { get; init; }

    public IReadOnlyList<string> Scenarios { get; init; } = [];

    public IReadOnlyList<int> Seeds { get; init; } = [];

    public int? Ticks { get; init; }

    public IReadOnlyList<string> Models { get; init; } = [];

    public IReadOnlyDictionary<string, double> BaseParameters { get; init; } =
        new Dictionary<string, double>();

    public IReadOnlyDictionary<string, IReadOnlyList<double>> Sweep { get; init; } =
        new Dictionary<string, IReadOnlyList<double>>();

    public int RunCount { get; init; }

    public IReadOnlyList<StressSweepRunResponse> Runs { get; init; } = [];

    public IReadOnlyList<ParameterSweepBestWorstResponse> BestWorst { get; init; } = [];
}

public sealed class StressSweepRunResponse
{
    public int RunIndex { get; init; }

    public RunSummaryResponse Run { get; init; } = new();

    public string ScenarioName { get; init; } = "";

    public int Seed { get; init; }

    public int Ticks { get; init; }

    public string Model { get; init; } = "";

    public IReadOnlyDictionary<string, double> Parameters { get; init; } =
        new Dictionary<string, double>();

    public IReadOnlyList<MetricResponse> FinalMetrics { get; init; } = [];
}
