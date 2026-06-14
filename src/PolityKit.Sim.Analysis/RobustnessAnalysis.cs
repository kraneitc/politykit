namespace PolityKit.Sim.Analysis;

public static class RobustnessAnalysis
{
    public static IReadOnlyList<ModelRobustnessSummary> BuildModelSummaries(
        IReadOnlyList<StressSweepRunResult> runs,
        SensitivityReport? sensitivity = null)
    {
        ArgumentNullException.ThrowIfNull(runs);

        return runs
            .GroupBy(run => run.Model)
            .OrderBy(group => group.Key)
            .Select(group => BuildModelSummary(group.Key, group.ToArray(), sensitivity))
            .ToArray();
    }

    private static ModelRobustnessSummary BuildModelSummary(
        string model,
        IReadOnlyList<StressSweepRunResult> runs,
        SensitivityReport? sensitivity)
    {
        var collapsedRuns = runs
            .Select(run => new
            {
                Run = run,
                FirstCollapseTick = FirstCollapseTick(run),
                Recovered = run.CollapseEvents.Any(collapse => collapse.Collapsed && collapse.Recovered)
            })
            .Where(item => item.FirstCollapseTick is not null)
            .ToArray();
        var collapseTicks = collapsedRuns
            .Select(item => item.FirstCollapseTick!.Value)
            .Order()
            .ToArray();
        var scenarioScores = ScoreScenarios(runs);

        return new ModelRobustnessSummary(
            model,
            runs.Select(run => run.ScenarioName).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray(),
            runs.Select(run => run.Seed).Distinct().Order().ToArray(),
            runs.Count,
            Rate(collapsedRuns.Length, runs.Count),
            Median(collapseTicks),
            collapseTicks.Length == 0 ? null : collapseTicks[0],
            Rate(collapsedRuns.Count(item => item.Recovered), collapsedRuns.Length),
            WorstAffectedMetric(runs),
            MostSensitiveParameter(model, sensitivity),
            scenarioScores.FirstOrDefault()?.ScenarioName,
            scenarioScores.LastOrDefault()?.ScenarioName);
    }

    private static int? FirstCollapseTick(StressSweepRunResult run)
    {
        return run.CollapseEvents
            .Where(collapse => collapse.CollapseTick is not null)
            .Select(collapse => collapse.CollapseTick)
            .Order()
            .FirstOrDefault();
    }

    private static string? WorstAffectedMetric(IReadOnlyList<StressSweepRunResult> runs)
    {
        return runs
            .SelectMany(run => run.CollapseEvents.Where(collapse => collapse.Collapsed))
            .GroupBy(collapse => collapse.Metric)
            .Select(group => new
            {
                Metric = group.Key,
                CollapseCount = group.Count(),
                EarliestCollapseTick = group.Min(collapse => collapse.CollapseTick)
            })
            .OrderByDescending(group => group.CollapseCount)
            .ThenBy(group => group.EarliestCollapseTick)
            .ThenBy(group => group.Metric)
            .Select(group => group.Metric)
            .FirstOrDefault();
    }

    private static string? MostSensitiveParameter(string model, SensitivityReport? sensitivity)
    {
        return sensitivity?.Metrics
            .Where(metric => string.Equals(metric.Model, model, StringComparison.OrdinalIgnoreCase))
            .SelectMany(metric => metric.Parameters)
            .GroupBy(parameter => parameter.Parameter, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Parameter = group.Key,
                MaxOutcomeRange = group.Max(parameter => parameter.OutcomeRange),
                MaxCorrelation = group.Max(parameter => Math.Abs(parameter.CorrelationScore ?? 0))
            })
            .OrderByDescending(group => group.MaxOutcomeRange)
            .ThenByDescending(group => group.MaxCorrelation)
            .ThenBy(group => group.Parameter)
            .Select(group => group.Parameter)
            .FirstOrDefault();
    }

    private static IReadOnlyList<ScenarioScore> ScoreScenarios(IReadOnlyList<StressSweepRunResult> runs)
    {
        return runs
            .GroupBy(run => run.ScenarioName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ScenarioScore(
                group.Key,
                group
                    .SelectMany(run => run.FinalMetrics)
                    .Select(metric => HigherIsBetter(metric.Name) ? metric.Value : -metric.Value)
                    .DefaultIfEmpty(0)
                    .Average()))
            .OrderByDescending(score => score.Score)
            .ThenBy(score => score.ScenarioName)
            .ToArray();
    }

    private static double Rate(int numerator, int denominator)
    {
        return denominator == 0 ? 0 : (double)numerator / denominator;
    }

    private static double? Median(IReadOnlyList<int> sortedValues)
    {
        if (sortedValues.Count == 0)
        {
            return null;
        }

        var middle = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 1
            ? sortedValues[middle]
            : (sortedValues[middle - 1] + sortedValues[middle]) / 2.0;
    }

    private static bool HigherIsBetter(string metricName)
    {
        return metricName is "Needs Met" or "Trust";
    }

    private sealed record ScenarioScore(string ScenarioName, double Score);
}

public sealed record ModelRobustnessSummary(
    string Model,
    IReadOnlyList<string> ScenariosTested,
    IReadOnlyList<int> SeedsTested,
    int RunsCompleted,
    double CollapseRate,
    double? MedianCollapseTick,
    int? EarliestCollapseTick,
    double RecoveryRate,
    string? WorstAffectedMetric,
    string? MostSensitiveParameter,
    string? BestScenarioName,
    string? WorstScenarioName);
