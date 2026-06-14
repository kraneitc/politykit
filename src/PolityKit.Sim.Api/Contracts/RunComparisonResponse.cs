namespace PolityKit.Sim.Api.Contracts;

public sealed class RunComparisonResponse
{
    public RunSummaryResponse Baseline { get; init; } = new();

    public RunSummaryResponse Comparison { get; init; } = new();

    public IReadOnlyList<MetricComparisonResponse> MetricDeltas { get; init; } = [];
}

public sealed class MetricComparisonResponse
{
    public string Model { get; init; } = "";

    public string Metric { get; init; } = "";

    public string Unit { get; init; } = "";

    public int? BaselineTick { get; init; }

    public int? ComparisonTick { get; init; }

    public double? BaselineValue { get; init; }

    public double? ComparisonValue { get; init; }

    public double? Change { get; init; }

    public double? PercentChange { get; init; }

    public string Direction { get; init; } = "unavailable";
}
