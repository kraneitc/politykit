using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Api.Services.Models;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Api.Services;

public static class RunMappers
{
    public static RunSummaryResponse ToSummaryResponse(StoredRun run)
    {
        return new RunSummaryResponse
        {
            Id = run.Id,
            CreatedAt = run.CreatedAt,
            ScenarioName = run.Result.ScenarioName,
            Seed = run.Result.Seed,
            Ticks = run.Result.Ticks,
            Models = run.Result.ModelResults.Select(model => model.ModelName).ToArray()
        };
    }

    public static RunDetailResponse ToDetailResponse(StoredRun run)
    {
        return new RunDetailResponse
        {
            Id = run.Id,
            CreatedAt = run.CreatedAt,
            ScenarioName = run.Result.ScenarioName,
            Seed = run.Result.Seed,
            Ticks = run.Result.Ticks,
            Models = run.Result.ModelResults.Select(model => new ModelRunSummaryResponse
            {
                ModelName = model.ModelName,
                ModelVersion = model.ModelVersion,
                EventCount = model.Events.Count,
                FinalMetrics = model.Metrics
                    .GroupBy(metric => metric.Name)
                    .Select(group => group.OrderByDescending(metric => metric.Tick).First())
                    .OrderBy(metric => metric.Name)
                    .Select(metric => new MetricResponse
                    {
                        Model = model.ModelName,
                        Tick = metric.Tick,
                        Name = metric.Name,
                        Value = metric.Value,
                        Unit = metric.Unit
                    })
                    .ToArray()
            }).ToArray()
        };
    }

    public static RunDashboardResponse ToDashboardResponse(StoredRun run)
    {
        return new RunDashboardResponse
        {
            Id = run.Id,
            CreatedAt = run.CreatedAt,
            ScenarioName = run.Result.ScenarioName,
            Seed = run.Result.Seed,
            Ticks = run.Result.Ticks,
            Summary = SimulationRunSummary.Create(run.Result),
            Metrics = ToMetrics(run),
            Events = ToEvents(run)
        };
    }

    public static RunComparisonResponse ToComparisonResponse(StoredRun baseline, StoredRun comparison)
    {
        var baselineMetrics = SelectFinalMetrics(baseline);
        var comparisonMetrics = SelectFinalMetrics(comparison);
        var metricKeys = baselineMetrics.Keys
            .Union(comparisonMetrics.Keys)
            .OrderBy(key => key.Model)
            .ThenBy(key => key.Metric)
            .ToArray();

        return new RunComparisonResponse
        {
            Baseline = ToSummaryResponse(baseline),
            Comparison = ToSummaryResponse(comparison),
            MetricDeltas = metricKeys
                .Select(key => ToMetricComparison(
                    key,
                    baselineMetrics.GetValueOrDefault(key),
                    comparisonMetrics.GetValueOrDefault(key)))
                .ToArray()
        };
    }

    public static IReadOnlyList<MetricResponse> ToMetrics(StoredRun run)
    {
        return run.Result.ModelResults
            .SelectMany(model => model.Metrics.Select(metric => new MetricResponse
            {
                Model = model.ModelName,
                Tick = metric.Tick,
                Name = metric.Name,
                Value = metric.Value,
                Unit = metric.Unit
            }))
            .ToArray();
    }

    public static IReadOnlyList<MetricResponse> ToFinalMetrics(StoredRun run)
    {
        return run.Result.ModelResults
            .SelectMany(model => model.Metrics
                .GroupBy(metric => metric.Name)
                .Select(group => group.OrderByDescending(metric => metric.Tick).First())
                .OrderBy(metric => metric.Name)
                .Select(metric => new MetricResponse
                {
                    Model = model.ModelName,
                    Tick = metric.Tick,
                    Name = metric.Name,
                    Value = metric.Value,
                    Unit = metric.Unit
                }))
            .ToArray();
    }

    public static IReadOnlyList<EventResponse> ToEvents(StoredRun run)
    {
        return run.Result.ModelResults
            .SelectMany(model => model.Events.Select(simEvent => new EventResponse
            {
                Model = model.ModelName,
                Tick = simEvent.Tick,
                Type = simEvent.Type,
                Description = simEvent.Description,
                Data = simEvent.Data
            }))
            .ToArray();
    }

    private static MetricComparisonResponse ToMetricComparison(
        MetricKey key,
        MetricResult? baselineMetric,
        MetricResult? comparisonMetric)
    {
        double? change = baselineMetric is null || comparisonMetric is null
            ? null
            : comparisonMetric.Value - baselineMetric.Value;
        double? percentChange = change is null || baselineMetric is null || baselineMetric.Value == 0
            ? null
            : change.Value / Math.Abs(baselineMetric.Value);

        return new MetricComparisonResponse
        {
            Model = key.Model,
            Metric = key.Metric,
            Unit = comparisonMetric?.Unit ?? baselineMetric?.Unit ?? "",
            BaselineTick = baselineMetric?.Tick,
            ComparisonTick = comparisonMetric?.Tick,
            BaselineValue = baselineMetric?.Value,
            ComparisonValue = comparisonMetric?.Value,
            Change = change,
            PercentChange = percentChange,
            Direction = Direction(change)
        };
    }

    private static IReadOnlyDictionary<MetricKey, MetricResult> SelectFinalMetrics(StoredRun run)
    {
        return run.Result.ModelResults
            .SelectMany(model => model.Metrics
                .GroupBy(metric => metric.Name)
                .Select(group => new
                {
                    Key = new MetricKey(model.ModelName, group.Key),
                    Metric = group.OrderByDescending(metric => metric.Tick).First()
                }))
            .ToDictionary(item => item.Key, item => item.Metric);
    }

    private static string Direction(double? change)
    {
        return change switch
        {
            null => "unavailable",
            > 0 => "increased",
            < 0 => "decreased",
            _ => "unchanged"
        };
    }

    private sealed record MetricKey(string Model, string Metric);
}
