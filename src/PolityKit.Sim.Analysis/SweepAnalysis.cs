using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Analysis;

public static class SweepAnalysis
{
    public const int DefaultMaxRuns = 256;

    public static IReadOnlyDictionary<string, IReadOnlyList<double>> NormalizeSweep(
        IReadOnlyDictionary<string, IReadOnlyList<double>>? sweep,
        int maxRuns = DefaultMaxRuns)
    {
        if (sweep is null || sweep.Count == 0)
        {
            throw new InvalidOperationException("At least one sweep parameter is required.");
        }

        var normalized = new Dictionary<string, IReadOnlyList<double>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, values) in sweep.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Sweep parameter names cannot be blank.");
            }

            if (values is null || values.Count == 0)
            {
                throw new InvalidOperationException($"Sweep parameter '{name}' must include at least one value.");
            }

            normalized[name] = values.ToArray();
        }

        var runCount = GetRunCount(normalized);
        if (runCount > maxRuns)
        {
            throw new InvalidOperationException($"Sweep would create {runCount} runs; the maximum is {maxRuns}.");
        }

        return normalized;
    }

    public static IReadOnlyList<IReadOnlyDictionary<string, double>> BuildParameterCombinations(
        IReadOnlyDictionary<string, double> baseParameters,
        IReadOnlyDictionary<string, IReadOnlyList<double>> sweep,
        int maxRuns = DefaultMaxRuns)
    {
        ArgumentNullException.ThrowIfNull(baseParameters);

        var normalized = NormalizeSweep(sweep, maxRuns);
        var combinations = new List<Dictionary<string, double>>
        {
            new(baseParameters, StringComparer.OrdinalIgnoreCase)
        };

        foreach (var (name, values) in normalized)
        {
            combinations = combinations
                .SelectMany(existing => values.Select(value =>
                {
                    var next = new Dictionary<string, double>(existing, StringComparer.OrdinalIgnoreCase)
                    {
                        [name] = value
                    };
                    return next;
                }))
                .ToList();
        }

        return combinations;
    }

    public static IReadOnlyList<SweepMetricReport> SelectFinalMetrics(SimulationRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.ModelResults
            .SelectMany(model => model.Metrics
                .GroupBy(metric => metric.Name)
                .Select(group => group.OrderByDescending(metric => metric.Tick).First())
                .OrderBy(metric => metric.Name)
                .Select(metric => new SweepMetricReport(
                    model.ModelName,
                    metric.Tick,
                    metric.Name,
                    metric.Value,
                    metric.Unit)))
            .ToArray();
    }

    public static IReadOnlyList<SweepBestWorstReport> BuildBestWorst(IReadOnlyList<SweepRunReport> sweepRuns)
    {
        ArgumentNullException.ThrowIfNull(sweepRuns);

        return sweepRuns
            .SelectMany(run => run.FinalMetrics.Select(metric => new
            {
                Run = run,
                Metric = metric
            }))
            .GroupBy(item => new
            {
                item.Metric.Model,
                item.Metric.Name,
                item.Metric.Unit
            })
            .OrderBy(group => group.Key.Model)
            .ThenBy(group => group.Key.Name)
            .Select(group =>
            {
                var higherIsBetter = HigherIsBetter(group.Key.Name);
                var best = higherIsBetter
                    ? group.MaxBy(item => item.Metric.Value)!
                    : group.MinBy(item => item.Metric.Value)!;
                var worst = higherIsBetter
                    ? group.MinBy(item => item.Metric.Value)!
                    : group.MaxBy(item => item.Metric.Value)!;
                return new SweepBestWorstReport(
                    group.Key.Model,
                    group.Key.Name,
                    group.Key.Unit,
                    higherIsBetter ? "higher" : "lower",
                    ToSweepMetricRun(best.Run, best.Metric),
                    ToSweepMetricRun(worst.Run, worst.Metric));
            })
            .ToArray();
    }

    private static int GetRunCount(IReadOnlyDictionary<string, IReadOnlyList<double>> sweep)
    {
        return sweep.Values.Aggregate(1, (count, values) => count * values.Count);
    }

    private static bool HigherIsBetter(string metricName)
    {
        return metricName is "Needs Met" or "Trust";
    }

    private static SweepMetricRunReport ToSweepMetricRun(SweepRunReport run, SweepMetricReport metric)
    {
        return new SweepMetricRunReport(
            run.RunIndex,
            run.Directory,
            metric.Value,
            run.Parameters);
    }
}

public sealed record SweepRunReport(
    int RunIndex,
    string? Directory,
    IReadOnlyDictionary<string, double> Parameters,
    IReadOnlyList<SweepMetricReport> FinalMetrics);

public sealed record SweepMetricReport(
    string Model,
    int Tick,
    string Name,
    double Value,
    string Unit);

public sealed record SweepBestWorstReport(
    string Model,
    string Metric,
    string Unit,
    string BestDirection,
    SweepMetricRunReport Best,
    SweepMetricRunReport Worst);

public sealed record SweepMetricRunReport(
    int RunIndex,
    string? Directory,
    double Value,
    IReadOnlyDictionary<string, double> Parameters);
