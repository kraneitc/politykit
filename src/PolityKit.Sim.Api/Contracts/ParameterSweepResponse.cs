namespace PolityKit.Sim.Api.Contracts;

public sealed class ParameterSweepResponse
{
    public string ScenarioName { get; init; } = "";

    public int Seed { get; init; }

    public int Ticks { get; init; }

    public int RunCount { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<double>> Sweep { get; init; } =
        new Dictionary<string, IReadOnlyList<double>>();

    public IReadOnlyList<ParameterSweepRunResponse> Runs { get; init; } = [];
}

public sealed class ParameterSweepRunResponse
{
    public RunSummaryResponse Run { get; init; } = new();

    public IReadOnlyDictionary<string, double> Parameters { get; init; } =
        new Dictionary<string, double>();

    public IReadOnlyList<MetricResponse> FinalMetrics { get; init; } = [];
}
