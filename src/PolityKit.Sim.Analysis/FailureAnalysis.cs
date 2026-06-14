using System.Text.Json.Serialization;
using PolityKit.Sim.Engine;
using PolityKit.Sim.Core.Metrics;

namespace PolityKit.Sim.Analysis;

public static class FailureAnalysis
{
    public static IReadOnlyList<FailureCriterion> DefaultCriteria { get; } =
    [
        new("Needs Met", FailureOperator.LessThan, 0.5, FailureThresholdKind.Absolute, RecoveryTicks: 1),
        new("Trust", FailureOperator.LessThan, 25, FailureThresholdKind.Absolute, RecoveryTicks: 1),
        new("Severe Failures", FailureOperator.GreaterThanOrEqual, 0.10, FailureThresholdKind.PopulationShare, RecoveryTicks: 1),
        new("Administrative Load", FailureOperator.GreaterThanOrEqual, 8, FailureThresholdKind.Absolute, RecoveryTicks: 1)
    ];

    public static IReadOnlyList<CollapseEvent> DetectCollapses(
        SimulationRunResult result,
        IReadOnlyList<FailureCriterion>? criteria = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        var activeCriteria = NormalizeCriteria(criteria);
        return result.ModelResults
            .SelectMany(model => activeCriteria.Select(criterion => DetectCollapse(model, criterion)))
            .ToArray();
    }

    private static CollapseEvent DetectCollapse(ModelRunResult model, FailureCriterion criterion)
    {
        var threshold = ResolveThreshold(criterion, model);
        var metrics = model.Metrics
            .Where(metric => string.Equals(metric.Name, criterion.Metric, StringComparison.OrdinalIgnoreCase))
            .OrderBy(metric => metric.Tick)
            .ToArray();
        var crossings = metrics
            .Where(metric => IsFailure(metric.Value, threshold, criterion.Operator))
            .Select(metric => new ThresholdCrossing(
                model.ModelName,
                criterion.Metric,
                criterion.Name,
                metric.Tick,
                metric.Value,
                threshold,
                criterion.Operator))
            .ToArray();

        var firstCrossing = crossings.FirstOrDefault();
        if (firstCrossing is null)
        {
            return CollapseEvent.NotObserved(
                model.ModelName,
                criterion.Metric,
                criterion.Name,
                threshold,
                criterion.Operator,
                crossings);
        }

        var recovery = FindRecovery(metrics, firstCrossing.Tick, threshold, criterion);
        return new CollapseEvent(
            model.ModelName,
            criterion.Metric,
            criterion.Name,
            threshold,
            criterion.Operator,
            firstCrossing.Tick,
            firstCrossing.Value,
            recovery?.Tick,
            recovery?.Value,
            recovery is not null ? "recovered" : "no recovery observed",
            crossings);
    }

    private static IReadOnlyList<FailureCriterion> NormalizeCriteria(IReadOnlyList<FailureCriterion>? criteria)
    {
        if (criteria is null || criteria.Count == 0)
        {
            return DefaultCriteria;
        }

        foreach (var criterion in criteria)
        {
            if (string.IsNullOrWhiteSpace(criterion.Metric))
            {
                throw new InvalidOperationException("Failure criterion metric names cannot be blank.");
            }

            if (criterion.RecoveryTicks < 1)
            {
                throw new InvalidOperationException($"Failure criterion '{criterion.Name}' must require at least one recovery tick.");
            }

            if (criterion.ThresholdKind == FailureThresholdKind.PopulationShare && criterion.Threshold < 0)
            {
                throw new InvalidOperationException($"Failure criterion '{criterion.Name}' population share threshold cannot be negative.");
            }
        }

        return criteria;
    }

    private static SweepMetricReport? FindRecovery(
        IReadOnlyList<MetricResult> metrics,
        int collapseTick,
        double threshold,
        FailureCriterion criterion)
    {
        var consecutiveRecovered = 0;
        foreach (var metric in metrics.Where(metric => metric.Tick > collapseTick))
        {
            if (IsFailure(metric.Value, threshold, criterion.Operator))
            {
                consecutiveRecovered = 0;
                continue;
            }

            consecutiveRecovered++;
            if (consecutiveRecovered >= criterion.RecoveryTicks)
            {
                return new SweepMetricReport("", metric.Tick, metric.Name, metric.Value, metric.Unit);
            }
        }

        return null;
    }

    private static double ResolveThreshold(FailureCriterion criterion, ModelRunResult model)
    {
        if (criterion.ThresholdKind == FailureThresholdKind.Absolute)
        {
            return criterion.Threshold;
        }

        var population = model.World.Population.Count;
        return population == 0
            ? 1
            : Math.Max(1, Math.Ceiling(population * criterion.Threshold));
    }

    private static bool IsFailure(double value, double threshold, FailureOperator failureOperator)
    {
        return failureOperator switch
        {
            FailureOperator.LessThan => value < threshold,
            FailureOperator.LessThanOrEqual => value <= threshold,
            FailureOperator.GreaterThan => value > threshold,
            FailureOperator.GreaterThanOrEqual => value >= threshold,
            _ => throw new InvalidOperationException($"Unknown failure operator '{failureOperator}'.")
        };
    }
}

public sealed record FailureCriterion(
    string Metric,
    FailureOperator Operator,
    double Threshold,
    FailureThresholdKind ThresholdKind = FailureThresholdKind.Absolute,
    int RecoveryTicks = 1,
    string? Label = null)
{
    public string Name => string.IsNullOrWhiteSpace(Label) ? Metric : Label;
}

[JsonConverter(typeof(JsonStringEnumConverter<FailureOperator>))]
public enum FailureOperator
{
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual
}

[JsonConverter(typeof(JsonStringEnumConverter<FailureThresholdKind>))]
public enum FailureThresholdKind
{
    Absolute,
    PopulationShare
}

public sealed record ThresholdCrossing(
    string Model,
    string Metric,
    string Criterion,
    int Tick,
    double Value,
    double Threshold,
    FailureOperator Operator);

public sealed record CollapseEvent(
    string Model,
    string Metric,
    string Criterion,
    double Threshold,
    FailureOperator Operator,
    int? CollapseTick,
    double? CollapseValue,
    int? RecoveryTick,
    double? RecoveryValue,
    string RecoveryStatus,
    IReadOnlyList<ThresholdCrossing> Crossings)
{
    public bool Collapsed => CollapseTick is not null;

    public bool Recovered => RecoveryTick is not null;

    public static CollapseEvent NotObserved(
        string model,
        string metric,
        string criterion,
        double threshold,
        FailureOperator failureOperator,
        IReadOnlyList<ThresholdCrossing> crossings)
    {
        return new CollapseEvent(
            model,
            metric,
            criterion,
            threshold,
            failureOperator,
            null,
            null,
            null,
            null,
            "no collapse observed",
            crossings);
    }
}
