using System.Text.Json;
using System.Text.Json.Serialization;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Analysis;

public static class AiAnalysisContextBuilders
{
    public const string RunSummaryPromptTemplateVersion = "run-summary-context-v1";
    public const string RunComparisonPromptTemplateVersion = "run-comparison-context-v1";
    public const string StressSummaryPromptTemplateVersion = "stress-summary-context-v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AiAnalysisRequest BuildRunSummaryRequest(
        SimulationRunResult result,
        IReadOnlyDictionary<string, double>? selectedParameters = null,
        IReadOnlyList<string>? relevantAssumptions = null,
        Guid? runId = null,
        IReadOnlyList<string>? sourceFiles = null,
        string promptTemplateVersion = RunSummaryPromptTemplateVersion)
    {
        ArgumentNullException.ThrowIfNull(result);

        var summary = SimulationRunSummary.Create(result);
        var modelNames = summary.Models
            .Select(model => model.ModelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var metricNames = summary.Models
            .SelectMany(model => model.FinalMetrics.Select(metric => metric.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var context = new RunSummaryContext(
            "single-run",
            AiAnalysisUsage.AdvisoryOutputRule,
            RedactionNotes,
            runId,
            summary.ScenarioName,
            summary.Seed,
            summary.Ticks,
            ToParameterValues(selectedParameters),
            NormalizeStrings(relevantAssumptions),
            summary.Models
                .OrderBy(model => model.ModelName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(model => model.ModelVersion, StringComparer.OrdinalIgnoreCase)
                .Select(ToModelContext)
                .ToArray());

        return new AiAnalysisRequest(
            AiAnalysisKind.RunSummary,
            Serialize(context),
            new AiAnalysisProvenance(
                runId is null ? [] : [runId.Value],
                NormalizeStrings(sourceFiles),
                [summary.ScenarioName],
                modelNames,
                [summary.Seed],
                metricNames,
                promptTemplateVersion,
                null,
                null,
                null));
    }

    public static AiAnalysisRequest BuildComparisonRequest(
        AiRunComparisonContext comparison,
        string promptTemplateVersion = RunComparisonPromptTemplateVersion)
    {
        ArgumentNullException.ThrowIfNull(comparison);

        var runIds = new[] { comparison.Baseline.RunId, comparison.Comparison.RunId }
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .Order()
            .ToArray();
        var scenarioNames = new[] { comparison.Baseline.ScenarioName, comparison.Comparison.ScenarioName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var modelNames = comparison.Baseline.Models
            .Concat(comparison.Comparison.Models)
            .Concat(comparison.MetricDeltas.Select(delta => delta.Model))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var metricNames = comparison.MetricDeltas
            .Select(delta => delta.Metric)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var seeds = new[] { comparison.Baseline.Seed, comparison.Comparison.Seed }
            .Distinct()
            .Order()
            .ToArray();
        var context = new ComparisonContext(
            "run-comparison",
            AiAnalysisUsage.AdvisoryOutputRule,
            RedactionNotes,
            ToRunReferenceContext(comparison.Baseline),
            ToRunReferenceContext(comparison.Comparison),
            comparison.MetricDeltas
                .OrderBy(delta => delta.Model, StringComparer.OrdinalIgnoreCase)
                .ThenBy(delta => delta.Metric, StringComparer.OrdinalIgnoreCase)
                .ThenBy(delta => delta.Unit, StringComparer.OrdinalIgnoreCase)
                .Select(delta => new MetricDeltaContext(
                    delta.Model,
                    delta.Metric,
                    delta.Unit,
                    delta.BaselineTick,
                    delta.ComparisonTick,
                    delta.BaselineValue,
                    delta.ComparisonValue,
                    delta.Change,
                    delta.PercentChange,
                    delta.Direction))
                .ToArray());

        return new AiAnalysisRequest(
            AiAnalysisKind.RunSummary,
            Serialize(context),
            new AiAnalysisProvenance(
                runIds,
                [],
                scenarioNames,
                modelNames,
                seeds,
                metricNames,
                promptTemplateVersion,
                null,
                null,
                null));
    }

    public static AiAnalysisRequest BuildStressSummaryRequest(
        StressSweepResult result,
        string promptTemplateVersion = StressSummaryPromptTemplateVersion)
    {
        ArgumentNullException.ThrowIfNull(result);

        var runIds = result.Runs
            .Select(run => run.RunId)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .Order()
            .ToArray();
        var scenarioNames = result.Scenarios
            .Concat(result.Runs.Select(run => run.ScenarioName))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var modelNames = result.Models
            .Concat(result.Runs.Select(run => run.Model))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var metricNames = result.Runs
            .SelectMany(run => run.FinalMetrics.Select(metric => metric.Name))
            .Concat(result.CollapseEvents.Select(collapse => collapse.Metric))
            .Concat(result.Sensitivity.Metrics.Select(metric => metric.Metric))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var context = new StressSummaryContext(
            "stress-summary",
            AiAnalysisUsage.AdvisoryOutputRule,
            RedactionNotes,
            result.GridName,
            scenarioNames,
            result.Seeds.Distinct().Order().ToArray(),
            modelNames,
            ToParameterValues(result.BaseParameters),
            result.Sweep
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => new SweepParameterValues(item.Key, item.Value.Order().ToArray()))
                .ToArray(),
            result.RunCount,
            result.Runs
                .OrderBy(run => run.RunIndex)
                .Select(ToStressRunContext)
                .ToArray(),
            result.BestWorst
                .OrderBy(report => report.Model, StringComparer.OrdinalIgnoreCase)
                .ThenBy(report => report.Metric, StringComparer.OrdinalIgnoreCase)
                .Select(ToBestWorstContext)
                .ToArray(),
            result.CollapseEvents
                .OrderBy(collapse => collapse.Model, StringComparer.OrdinalIgnoreCase)
                .ThenBy(collapse => collapse.Metric, StringComparer.OrdinalIgnoreCase)
                .ThenBy(collapse => collapse.Criterion, StringComparer.OrdinalIgnoreCase)
                .ThenBy(collapse => collapse.CollapseTick)
                .Select(ToCollapseContext)
                .ToArray(),
            result.Sensitivity.Metrics
                .OrderBy(metric => metric.ScenarioName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(metric => metric.Model, StringComparer.OrdinalIgnoreCase)
                .ThenBy(metric => metric.Metric, StringComparer.OrdinalIgnoreCase)
                .Select(ToSensitivityContext)
                .ToArray(),
            result.ModelRobustness
                .OrderBy(summary => summary.Model, StringComparer.OrdinalIgnoreCase)
                .Select(summary => new ModelRobustnessContext(
                    summary.Model,
                    summary.ScenariosTested.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                    summary.SeedsTested.Order().ToArray(),
                    summary.RunsCompleted,
                    summary.CollapseRate,
                    summary.MedianCollapseTick,
                    summary.EarliestCollapseTick,
                    summary.RecoveryRate,
                    summary.WorstAffectedMetric,
                    summary.MostSensitiveParameter,
                    summary.BestScenarioName,
                    summary.WorstScenarioName))
                .ToArray());

        return new AiAnalysisRequest(
            AiAnalysisKind.BatchAnomalyReport,
            Serialize(context),
            new AiAnalysisProvenance(
                runIds,
                [],
                scenarioNames,
                modelNames,
                result.Seeds.Distinct().Order().ToArray(),
                metricNames,
                promptTemplateVersion,
                null,
                null,
                null));
    }

    private static IReadOnlyList<string> RedactionNotes =>
    [
        "Raw citizen state is excluded.",
        "Full event streams are excluded; only summary counts and notable nearby events are included.",
        "Raw event data values are excluded; notable events include sorted data key names only."
    ];

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static IReadOnlyList<string> NormalizeStrings(IReadOnlyList<string>? values)
    {
        return values is null
            ? []
            : values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    private static IReadOnlyList<ParameterValueContext> ToParameterValues(IReadOnlyDictionary<string, double>? parameters)
    {
        return parameters is null
            ? []
            : parameters
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => new ParameterValueContext(item.Key, item.Value))
                .ToArray();
    }

    private static ModelSummaryContext ToModelContext(ModelRunSummary model)
    {
        return new ModelSummaryContext(
            model.ModelName,
            model.ModelVersion,
            model.EventCount,
            model.EventCountsByType
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => new EventCountContext(item.Key, item.Value))
                .ToArray(),
            model.FinalMetrics
                .OrderBy(metric => metric.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(metric => metric.Tick)
                .Select(metric => new MetricContext(model.ModelName, metric.Tick, metric.Name, metric.Value, metric.Unit))
                .ToArray(),
            model.NotableMetricChanges
                .OrderBy(change => change.Metric, StringComparer.OrdinalIgnoreCase)
                .ThenBy(change => change.ToTick)
                .Select(change => new NotableMetricChangeContext(
                    change.Metric,
                    change.FromTick,
                    change.ToTick,
                    change.PreviousValue,
                    change.Value,
                    change.Change,
                    change.AbsoluteChange,
                    change.Direction,
                    change.Unit,
                    change.Breadcrumb,
                    change.NearbyEvents
                        .OrderBy(simEvent => simEvent.Tick)
                        .ThenBy(simEvent => simEvent.Type, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(simEvent => simEvent.Description, StringComparer.OrdinalIgnoreCase)
                        .Select(ToEventContext)
                        .ToArray()))
                .ToArray());
    }

    private static NotableEventContext ToEventContext(EventSummary simEvent)
    {
        return new NotableEventContext(
            simEvent.Tick,
            simEvent.Type,
            simEvent.Description,
            simEvent.Data.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static RunReferenceContext ToRunReferenceContext(AiRunReference run)
    {
        return new RunReferenceContext(
            run.RunId,
            run.CreatedAt,
            run.ScenarioName,
            run.Seed,
            run.Ticks,
            NormalizeStrings(run.Models));
    }

    private static StressRunContext ToStressRunContext(StressSweepRunResult run)
    {
        return new StressRunContext(
            run.RunIndex,
            run.Directory,
            run.RunId,
            run.ScenarioName,
            run.Seed,
            run.Ticks,
            run.Model,
            ToParameterValues(run.Parameters),
            run.FinalMetrics
                .OrderBy(metric => metric.Model, StringComparer.OrdinalIgnoreCase)
                .ThenBy(metric => metric.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(metric => metric.Tick)
                .Select(metric => new MetricContext(metric.Model, metric.Tick, metric.Name, metric.Value, metric.Unit))
                .ToArray(),
            run.CollapseEvents
                .OrderBy(collapse => collapse.Model, StringComparer.OrdinalIgnoreCase)
                .ThenBy(collapse => collapse.Metric, StringComparer.OrdinalIgnoreCase)
                .ThenBy(collapse => collapse.Criterion, StringComparer.OrdinalIgnoreCase)
                .Select(ToCollapseContext)
                .ToArray());
    }

    private static BestWorstContext ToBestWorstContext(SweepBestWorstReport report)
    {
        return new BestWorstContext(
            report.Model,
            report.Metric,
            report.Unit,
            report.BestDirection,
            ToMetricRunContext(report.Best),
            ToMetricRunContext(report.Worst));
    }

    private static MetricRunContext ToMetricRunContext(SweepMetricRunReport run)
    {
        return new MetricRunContext(
            run.RunIndex,
            run.Directory,
            run.Value,
            ToParameterValues(run.Parameters));
    }

    private static CollapseContext ToCollapseContext(CollapseEvent collapse)
    {
        return new CollapseContext(
            collapse.Model,
            collapse.Metric,
            collapse.Criterion,
            collapse.Threshold,
            collapse.Operator.ToString(),
            collapse.CollapseTick,
            collapse.CollapseValue,
            collapse.RecoveryTick,
            collapse.RecoveryValue,
            collapse.RecoveryStatus);
    }

    private static SensitivityContext ToSensitivityContext(MetricSensitivity metric)
    {
        return new SensitivityContext(
            metric.ScenarioName,
            metric.Model,
            metric.Metric,
            metric.Unit,
            metric.Parameters
                .OrderBy(parameter => parameter.Parameter, StringComparer.OrdinalIgnoreCase)
                .Select(parameter => new ParameterSensitivityContext(
                    parameter.Parameter,
                    parameter.SampleCount,
                    parameter.MinParameterValue,
                    parameter.MaxParameterValue,
                    parameter.MinOutcome,
                    parameter.MaxOutcome,
                    parameter.OutcomeRange,
                    parameter.BaselineParameterValue,
                    parameter.BaselineOutcome,
                    parameter.DeltaFromBaseline,
                    parameter.Direction,
                    parameter.CorrelationScore))
                .ToArray());
    }

    private sealed record RunSummaryContext(
        string SourceType,
        string BoundaryRule,
        IReadOnlyList<string> Exclusions,
        Guid? RunId,
        string ScenarioName,
        int Seed,
        int Ticks,
        IReadOnlyList<ParameterValueContext> SelectedParameters,
        IReadOnlyList<string> RelevantAssumptions,
        IReadOnlyList<ModelSummaryContext> Models);

    private sealed record ModelSummaryContext(
        string ModelName,
        string ModelVersion,
        int EventCount,
        IReadOnlyList<EventCountContext> EventCountsByType,
        IReadOnlyList<MetricContext> FinalMetrics,
        IReadOnlyList<NotableMetricChangeContext> NotableMetricChanges);

    private sealed record EventCountContext(string Type, int Count);

    private sealed record MetricContext(string Model, int Tick, string Name, double Value, string Unit);

    private sealed record NotableMetricChangeContext(
        string Metric,
        int FromTick,
        int ToTick,
        double PreviousValue,
        double Value,
        double Change,
        double AbsoluteChange,
        string Direction,
        string Unit,
        string Breadcrumb,
        IReadOnlyList<NotableEventContext> NearbyEvents);

    private sealed record NotableEventContext(
        int Tick,
        string Type,
        string Description,
        IReadOnlyList<string> DataKeys);

    private sealed record ComparisonContext(
        string SourceType,
        string BoundaryRule,
        IReadOnlyList<string> Exclusions,
        RunReferenceContext Baseline,
        RunReferenceContext Comparison,
        IReadOnlyList<MetricDeltaContext> MetricDeltas);

    private sealed record RunReferenceContext(
        Guid? RunId,
        DateTimeOffset? CreatedAt,
        string ScenarioName,
        int Seed,
        int Ticks,
        IReadOnlyList<string> Models);

    private sealed record MetricDeltaContext(
        string Model,
        string Metric,
        string Unit,
        int? BaselineTick,
        int? ComparisonTick,
        double? BaselineValue,
        double? ComparisonValue,
        double? Change,
        double? PercentChange,
        string Direction);

    private sealed record StressSummaryContext(
        string SourceType,
        string BoundaryRule,
        IReadOnlyList<string> Exclusions,
        string? GridName,
        IReadOnlyList<string> Scenarios,
        IReadOnlyList<int> Seeds,
        IReadOnlyList<string> Models,
        IReadOnlyList<ParameterValueContext> BaseParameters,
        IReadOnlyList<SweepParameterValues> Sweep,
        int RunCount,
        IReadOnlyList<StressRunContext> Runs,
        IReadOnlyList<BestWorstContext> BestWorst,
        IReadOnlyList<CollapseContext> CollapseEvents,
        IReadOnlyList<SensitivityContext> Sensitivity,
        IReadOnlyList<ModelRobustnessContext> ModelRobustness);

    private sealed record StressRunContext(
        int RunIndex,
        string? Directory,
        Guid? RunId,
        string ScenarioName,
        int Seed,
        int Ticks,
        string Model,
        IReadOnlyList<ParameterValueContext> Parameters,
        IReadOnlyList<MetricContext> FinalMetrics,
        IReadOnlyList<CollapseContext> CollapseEvents);

    private sealed record ParameterValueContext(string Name, double Value);

    private sealed record SweepParameterValues(string Name, IReadOnlyList<double> Values);

    private sealed record BestWorstContext(
        string Model,
        string Metric,
        string Unit,
        string BestDirection,
        MetricRunContext Best,
        MetricRunContext Worst);

    private sealed record MetricRunContext(
        int RunIndex,
        string? Directory,
        double Value,
        IReadOnlyList<ParameterValueContext> Parameters);

    private sealed record CollapseContext(
        string Model,
        string Metric,
        string Criterion,
        double Threshold,
        string Operator,
        int? CollapseTick,
        double? CollapseValue,
        int? RecoveryTick,
        double? RecoveryValue,
        string RecoveryStatus);

    private sealed record SensitivityContext(
        string ScenarioName,
        string Model,
        string Metric,
        string Unit,
        IReadOnlyList<ParameterSensitivityContext> Parameters);

    private sealed record ParameterSensitivityContext(
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

    private sealed record ModelRobustnessContext(
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
}

public sealed record AiRunComparisonContext(
    AiRunReference Baseline,
    AiRunReference Comparison,
    IReadOnlyList<AiMetricDeltaContext> MetricDeltas);

public sealed record AiRunReference(
    Guid? RunId,
    DateTimeOffset? CreatedAt,
    string ScenarioName,
    int Seed,
    int Ticks,
    IReadOnlyList<string> Models);

public sealed record AiMetricDeltaContext(
    string Model,
    string Metric,
    string Unit,
    int? BaselineTick,
    int? ComparisonTick,
    double? BaselineValue,
    double? ComparisonValue,
    double? Change,
    double? PercentChange,
    string Direction);
