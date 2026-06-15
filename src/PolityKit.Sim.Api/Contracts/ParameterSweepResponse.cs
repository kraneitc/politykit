using PolityKit.Sim.Analysis;

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

    public IReadOnlyList<ParameterSweepBestWorstResponse> BestWorst { get; init; } = [];

    public SensitivityReport Sensitivity { get; init; } = new([]);

    public AiAnalysisUsage AiAnalysis { get; init; } = AiAnalysisUsage.NotUsed();
}

public sealed class ParameterSweepRunResponse
{
    public RunSummaryResponse Run { get; init; } = new();

    public IReadOnlyDictionary<string, double> Parameters { get; init; } =
        new Dictionary<string, double>();

    public IReadOnlyList<MetricResponse> FinalMetrics { get; init; } = [];
}

public sealed class ParameterSweepBestWorstResponse
{
    public string Model { get; init; } = "";

    public string Metric { get; init; } = "";

    public string Unit { get; init; } = "";

    public string BestDirection { get; init; } = "";

    public ParameterSweepMetricRunResponse Best { get; init; } = new();

    public ParameterSweepMetricRunResponse Worst { get; init; } = new();
}

public sealed class ParameterSweepMetricRunResponse
{
    public int RunIndex { get; init; }

    public string? Directory { get; init; }

    public double Value { get; init; }

    public IReadOnlyDictionary<string, double> Parameters { get; init; } =
        new Dictionary<string, double>();
}
