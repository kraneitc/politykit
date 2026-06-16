using System.Text.Json;
using System.Text.Json.Serialization;
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Analysis;

public static class AiAnalysisContextBuilders
{
    public const string RunSummaryPromptTemplateVersion = "run-summary-context-v1";
    public const string RunComparisonPromptTemplateVersion = "run-comparison-context-v1";
    public const string StressSummaryPromptTemplateVersion = "stress-summary-context-v1";
    public const string ModelCritiquePromptTemplateVersion = "model-critique-context-v1";
    public const string BatchAnomalyPromptTemplateVersion = "batch-anomaly-context-v1";

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

        return BuildRunSummaryRequest(
            SimulationRunSummary.Create(result),
            selectedParameters,
            relevantAssumptions,
            runId,
            sourceFiles,
            promptTemplateVersion);
    }

    public static AiAnalysisRequest BuildRunSummaryRequest(
        SimulationRunSummary summary,
        IReadOnlyDictionary<string, double>? selectedParameters = null,
        IReadOnlyList<string>? relevantAssumptions = null,
        Guid? runId = null,
        IReadOnlyList<string>? sourceFiles = null,
        string promptTemplateVersion = RunSummaryPromptTemplateVersion)
    {
        ArgumentNullException.ThrowIfNull(summary);

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
        return BuildStressSummaryRequest(
            result,
            "stress-summary",
            sourceFiles: null,
            promptTemplateVersion);
    }

    public static AiAnalysisRequest BuildBatchAnomalyRequest(
        StressSweepResult result,
        IReadOnlyList<string>? sourceFiles = null,
        string promptTemplateVersion = BatchAnomalyPromptTemplateVersion)
    {
        return BuildStressSummaryRequest(
            result,
            "batch-anomaly",
            sourceFiles,
            promptTemplateVersion);
    }

    private static AiAnalysisRequest BuildStressSummaryRequest(
        StressSweepResult result,
        string sourceType,
        IReadOnlyList<string>? sourceFiles,
        string promptTemplateVersion)
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
            sourceType,
            AiAnalysisUsage.AdvisoryOutputRule,
            sourceType == "batch-anomaly"
                ? [
                    .. RedactionNotes,
                    "Anomaly assistance must reference only runs, models, scenarios, seeds, and metrics present in this context.",
                    "Anomalies are advisory interpretations of deterministic batch summaries, not simulation data."
                ]
                : RedactionNotes,
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
                NormalizeStrings(sourceFiles),
                scenarioNames,
                modelNames,
                result.Seeds.Distinct().Order().ToArray(),
                metricNames,
                promptTemplateVersion,
                null,
                null,
                null));
    }

    public static AiAnalysisRequest BuildModelCritiqueRequest(
        IReadOnlyList<ModelManifest> manifests,
        SimulationRunSummary? runSummary = null,
        IReadOnlyList<CollapseEvent>? collapseEvents = null,
        StressSweepResult? stressResult = null,
        Guid? runId = null,
        IReadOnlyList<string>? sourceFiles = null,
        string promptTemplateVersion = ModelCritiquePromptTemplateVersion)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        if (manifests.Count == 0)
        {
            throw new InvalidOperationException("Model critique requires at least one model manifest.");
        }

        var orderedManifests = manifests
            .Where(manifest => !string.IsNullOrWhiteSpace(manifest.Model))
            .OrderBy(manifest => manifest.Model, StringComparer.OrdinalIgnoreCase)
            .ThenBy(manifest => manifest.Version, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (orderedManifests.Length == 0)
        {
            throw new InvalidOperationException("Model critique requires at least one named model manifest.");
        }

        var runModels = runSummary?.Models
            .Where(model => orderedManifests.Any(manifest =>
                string.Equals(manifest.Model, model.ModelName, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(model => model.ModelName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(model => model.ModelVersion, StringComparer.OrdinalIgnoreCase)
            .Select(ToModelContext)
            .ToArray() ?? [];
        var stressRuns = stressResult?.Runs
            .Where(run => orderedManifests.Any(manifest =>
                string.Equals(manifest.Model, run.Model, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(run => run.RunIndex)
            .Select(ToStressRunContext)
            .ToArray() ?? [];
        var robustness = stressResult?.ModelRobustness
            .Where(summary => orderedManifests.Any(manifest =>
                string.Equals(manifest.Model, summary.Model, StringComparison.OrdinalIgnoreCase)))
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
            .ToArray() ?? [];
        var collapses = (collapseEvents ?? [])
            .Concat(stressResult?.CollapseEvents ?? [])
            .Where(collapse => orderedManifests.Any(manifest =>
                string.Equals(manifest.Model, collapse.Model, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(collapse => collapse.Model, StringComparer.OrdinalIgnoreCase)
            .ThenBy(collapse => collapse.Metric, StringComparer.OrdinalIgnoreCase)
            .ThenBy(collapse => collapse.Criterion, StringComparer.OrdinalIgnoreCase)
            .ThenBy(collapse => collapse.CollapseTick)
            .Select(ToCollapseContext)
            .ToArray();
        var scenarioNames = NormalizeStrings(
            (runSummary is null ? Array.Empty<string>() : [runSummary.ScenarioName])
            .Concat(stressResult?.Scenarios ?? [])
            .ToArray());
        var seeds = (runSummary is null ? Array.Empty<int>() : [runSummary.Seed])
            .Concat(stressResult?.Seeds ?? [])
            .Distinct()
            .Order()
            .ToArray();
        var metricNames = runModels
            .SelectMany(model => model.FinalMetrics.Select(metric => metric.Name))
            .Concat(stressRuns.SelectMany(run => run.FinalMetrics.Select(metric => metric.Name)))
            .Concat(collapses.Select(collapse => collapse.Metric))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var context = new ModelCritiqueContext(
            "model-critique",
            AiAnalysisUsage.AdvisoryOutputRule,
            [
                .. RedactionNotes,
                "Critiques are prompts for human review, not proof that a model is correct or incorrect.",
                "The context excludes model source code and includes manifests plus deterministic summaries only."
            ],
            orderedManifests.Select(ToModelManifestContext).ToArray(),
            runSummary is null ? null : new RunReferenceContext(
                runId,
                null,
                runSummary.ScenarioName,
                runSummary.Seed,
                runSummary.Ticks,
                runModels.Select(model => model.ModelName).ToArray()),
            runModels,
            stressRuns,
            collapses,
            robustness);

        return new AiAnalysisRequest(
            AiAnalysisKind.ModelCritique,
            Serialize(context),
            new AiAnalysisProvenance(
                runId is null ? [] : [runId.Value],
                NormalizeStrings(sourceFiles),
                scenarioNames,
                orderedManifests.Select(manifest => manifest.Model).ToArray(),
                seeds,
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

    private static ModelManifestContext ToModelManifestContext(ModelManifest manifest)
    {
        return new ModelManifestContext(
            manifest.Model,
            manifest.Version,
            manifest.Description,
            manifest.Assumptions
                .OrderBy(assumption => assumption.Name, StringComparer.OrdinalIgnoreCase)
                .Select(assumption => new ModelAssumptionContext(
                    assumption.Name,
                    assumption.Default,
                    assumption.Description))
                .ToArray(),
            manifest.GovernanceDimensions
                .OrderBy(dimension => dimension.DimensionId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(dimension => dimension.ValueId, StringComparer.OrdinalIgnoreCase)
                .Select(dimension => new GovernanceDimensionContext(
                    dimension.DimensionId,
                    dimension.DimensionName,
                    dimension.ValueId,
                    dimension.ValueName,
                    dimension.Description,
                    dimension.Assumption,
                    ToParameterValues(dimension.Parameters),
                    dimension.KnownFailureModes.Order(StringComparer.OrdinalIgnoreCase).ToArray()))
                .ToArray(),
            manifest.KnownFailureModes.Order(StringComparer.OrdinalIgnoreCase).ToArray());
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

    private sealed record ModelCritiqueContext(
        string SourceType,
        string BoundaryRule,
        IReadOnlyList<string> Exclusions,
        IReadOnlyList<ModelManifestContext> Manifests,
        RunReferenceContext? Run,
        IReadOnlyList<ModelSummaryContext> RunModels,
        IReadOnlyList<StressRunContext> StressRuns,
        IReadOnlyList<CollapseContext> CollapseEvents,
        IReadOnlyList<ModelRobustnessContext> ModelRobustness);

    private sealed record ModelManifestContext(
        string Model,
        string Version,
        string Description,
        IReadOnlyList<ModelAssumptionContext> Assumptions,
        IReadOnlyList<GovernanceDimensionContext> GovernanceDimensions,
        IReadOnlyList<string> KnownFailureModes);

    private sealed record ModelAssumptionContext(string Name, double Default, string Description);

    private sealed record GovernanceDimensionContext(
        string DimensionId,
        string DimensionName,
        string ValueId,
        string ValueName,
        string Description,
        string Assumption,
        IReadOnlyList<ParameterValueContext> Parameters,
        IReadOnlyList<string> KnownFailureModes);

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
