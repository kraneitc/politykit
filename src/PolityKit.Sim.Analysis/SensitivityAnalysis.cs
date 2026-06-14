namespace PolityKit.Sim.Analysis;

public static class SensitivityAnalysis
{
    public static SensitivityReport BuildReport(
        string scenarioName,
        IReadOnlyList<SweepRunReport> runs,
        IReadOnlyDictionary<string, double>? baseParameters = null)
    {
        ArgumentNullException.ThrowIfNull(runs);

        return BuildReport(runs
            .Select(run => new SensitivityRunSample(
                scenarioName,
                run.RunIndex,
                run.Parameters,
                run.FinalMetrics))
            .ToArray(), baseParameters);
    }

    public static SensitivityReport BuildReport(
        IReadOnlyList<StressSweepRunResult> runs,
        IReadOnlyDictionary<string, double>? baseParameters = null)
    {
        ArgumentNullException.ThrowIfNull(runs);

        return BuildReport(runs
            .Select(run => new SensitivityRunSample(
                run.ScenarioName,
                run.RunIndex,
                run.Parameters,
                run.FinalMetrics))
            .ToArray(), baseParameters);
    }

    private static SensitivityReport BuildReport(
        IReadOnlyList<SensitivityRunSample> runs,
        IReadOnlyDictionary<string, double>? baseParameters)
    {
        var normalizedBaseParameters = baseParameters is null
            ? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, double>(baseParameters, StringComparer.OrdinalIgnoreCase);
        var metricSensitivities = runs
            .SelectMany(run => run.FinalMetrics.Select(metric => new
            {
                run.ScenarioName,
                run.RunIndex,
                run.Parameters,
                Metric = metric
            }))
            .GroupBy(item => new MetricSensitivityKey(
                item.ScenarioName,
                item.Metric.Model,
                item.Metric.Name,
                item.Metric.Unit))
            .OrderBy(group => group.Key.ScenarioName)
            .ThenBy(group => group.Key.Model)
            .ThenBy(group => group.Key.Metric)
            .Select(group => new MetricSensitivity(
                group.Key.ScenarioName,
                group.Key.Model,
                group.Key.Metric,
                group.Key.Unit,
                BuildParameterSensitivities(
                    group
                        .Select(item => new MetricSample(
                            item.RunIndex,
                            item.Parameters,
                            item.Metric.Value))
                        .ToArray(),
                    normalizedBaseParameters)))
            .ToArray();

        return new SensitivityReport(metricSensitivities);
    }

    private static IReadOnlyList<ParameterSensitivity> BuildParameterSensitivities(
        IReadOnlyList<MetricSample> samples,
        IReadOnlyDictionary<string, double> baseParameters)
    {
        var parameterNames = samples
            .SelectMany(sample => sample.Parameters.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return parameterNames
            .Select(parameter => BuildParameterSensitivity(parameter, samples, baseParameters))
            .OrderByDescending(sensitivity => sensitivity.OutcomeRange)
            .ThenByDescending(sensitivity => Math.Abs(sensitivity.CorrelationScore ?? 0))
            .ThenBy(sensitivity => sensitivity.Parameter, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ParameterSensitivity BuildParameterSensitivity(
        string parameter,
        IReadOnlyList<MetricSample> allSamples,
        IReadOnlyDictionary<string, double> baseParameters)
    {
        var samples = allSamples
            .Where(sample => sample.Parameters.ContainsKey(parameter))
            .Select(sample => new ParameterMetricSample(
                sample.RunIndex,
                sample.Parameters[parameter],
                sample.Outcome))
            .ToArray();
        var groupedMeans = samples
            .GroupBy(sample => sample.ParameterValue)
            .OrderBy(group => group.Key)
            .Select(group => new ParameterOutcomeMean(
                group.Key,
                group.Average(sample => sample.Outcome)))
            .ToArray();
        var minOutcome = groupedMeans.Min(group => group.MeanOutcome);
        var maxOutcome = groupedMeans.Max(group => group.MeanOutcome);

        var minParameterValue = groupedMeans.First().ParameterValue;
        var maxParameterValue = groupedMeans.Last().ParameterValue;
        var baselineParameterValue = baseParameters.GetValueOrDefault(parameter, minParameterValue);
        var baselineOutcome = groupedMeans
            .Where(group => NearlyEqual(group.ParameterValue, baselineParameterValue))
            .Select(group => (double?)group.MeanOutcome)
            .FirstOrDefault();
        var deltaFromBaseline = baselineOutcome is null
            ? (double?)null
            : groupedMeans.Last().MeanOutcome - baselineOutcome.Value;

        return new ParameterSensitivity(
            parameter,
            samples.Length,
            minParameterValue,
            maxParameterValue,
            minOutcome,
            maxOutcome,
            maxOutcome - minOutcome,
            baselineParameterValue,
            baselineOutcome,
            deltaFromBaseline,
            Direction(groupedMeans),
            Correlation(samples));
    }

    private static string Direction(IReadOnlyList<ParameterOutcomeMean> groupedMeans)
    {
        if (groupedMeans.Count < 2)
        {
            return "unavailable";
        }

        var differences = groupedMeans
            .Zip(groupedMeans.Skip(1), (left, right) => right.MeanOutcome - left.MeanOutcome)
            .Where(difference => !NearlyEqual(difference, 0))
            .ToArray();

        if (differences.Length == 0)
        {
            return "flat";
        }

        if (differences.All(difference => difference > 0))
        {
            return "increases";
        }

        return differences.All(difference => difference < 0)
            ? "decreases"
            : "mixed";
    }

    private static double? Correlation(IReadOnlyList<ParameterMetricSample> samples)
    {
        if (samples.Count < 2)
        {
            return null;
        }

        var parameterMean = samples.Average(sample => sample.ParameterValue);
        var outcomeMean = samples.Average(sample => sample.Outcome);
        var numerator = samples.Sum(sample =>
            (sample.ParameterValue - parameterMean) * (sample.Outcome - outcomeMean));
        var parameterVariance = samples.Sum(sample => Math.Pow(sample.ParameterValue - parameterMean, 2));
        var outcomeVariance = samples.Sum(sample => Math.Pow(sample.Outcome - outcomeMean, 2));
        var denominator = Math.Sqrt(parameterVariance * outcomeVariance);

        return NearlyEqual(denominator, 0)
            ? null
            : numerator / denominator;
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) < 0.000000001;
    }

    private sealed record SensitivityRunSample(
        string ScenarioName,
        int RunIndex,
        IReadOnlyDictionary<string, double> Parameters,
        IReadOnlyList<SweepMetricReport> FinalMetrics);

    private sealed record MetricSample(
        int RunIndex,
        IReadOnlyDictionary<string, double> Parameters,
        double Outcome);

    private sealed record ParameterMetricSample(
        int RunIndex,
        double ParameterValue,
        double Outcome);

    private sealed record ParameterOutcomeMean(
        double ParameterValue,
        double MeanOutcome);

    private sealed record MetricSensitivityKey(
        string ScenarioName,
        string Model,
        string Metric,
        string Unit);
}

public sealed record SensitivityReport(
    IReadOnlyList<MetricSensitivity> Metrics);

public sealed record MetricSensitivity(
    string ScenarioName,
    string Model,
    string Metric,
    string Unit,
    IReadOnlyList<ParameterSensitivity> Parameters);

public sealed record ParameterSensitivity(
    string Parameter,
    int SampleCount,
    double MinParameterValue,
    double MaxParameterValue,
    double MinOutcome,
    double MaxOutcome,
    double OutcomeRange,
    double? BaselineParameterValue,
    double? BaselineOutcome,
    double? DeltaFromBaseline,
    string Direction,
    double? CorrelationScore);
