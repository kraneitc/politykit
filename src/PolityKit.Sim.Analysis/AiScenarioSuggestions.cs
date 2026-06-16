using System.Text.Json;
using System.Text.Json.Serialization;
using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Analysis;

public static class AiScenarioSuggestionContextBuilder
{
    public const string PromptTemplateVersion = "scenario-suggestion-context-v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AiAnalysisRequest BuildRequest(
        ScenarioDefinition sourceScenario,
        SimulationRunResult runResult,
        IReadOnlyDictionary<string, double>? selectedParameters = null,
        IReadOnlyList<CollapseEvent>? observedFailures = null,
        StressSweepResult? stressResult = null,
        Guid? runId = null,
        IReadOnlyList<string>? sourceFiles = null,
        string promptTemplateVersion = PromptTemplateVersion)
    {
        ArgumentNullException.ThrowIfNull(sourceScenario);
        ArgumentNullException.ThrowIfNull(runResult);

        return BuildRequest(
            sourceScenario,
            SimulationRunSummary.Create(runResult),
            selectedParameters,
            observedFailures,
            stressResult,
            runId,
            sourceFiles,
            promptTemplateVersion);
    }

    public static AiAnalysisRequest BuildRequest(
        ScenarioDefinition sourceScenario,
        SimulationRunSummary summary,
        IReadOnlyDictionary<string, double>? selectedParameters = null,
        IReadOnlyList<CollapseEvent>? observedFailures = null,
        StressSweepResult? stressResult = null,
        Guid? runId = null,
        IReadOnlyList<string>? sourceFiles = null,
        string promptTemplateVersion = PromptTemplateVersion)
    {
        ArgumentNullException.ThrowIfNull(sourceScenario);
        ArgumentNullException.ThrowIfNull(summary);

        var metricNames = summary.Models
            .SelectMany(model => model.FinalMetrics.Select(metric => metric.Name))
            .Concat(stressResult?.Sensitivity.Metrics.Select(metric => metric.Metric) ?? [])
            .Concat(observedFailures?.Select(failure => failure.Metric) ?? [])
            .Where(metric => !string.IsNullOrWhiteSpace(metric))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var modelNames = summary.Models
            .Select(model => model.ModelName)
            .Concat(stressResult?.Models ?? [])
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var context = new ScenarioSuggestionContext(
            "scenario-suggestion",
            AiAnalysisUsage.AdvisoryOutputRule,
            [
                "Return a draft scenario only; do not treat it as accepted simulation data.",
                "The draft must pass existing scenario validation before it can be saved as a scenario file.",
                "Suggested changes should explain why the draft is useful for the next experiment."
            ],
            ValidationRules,
            ToScenarioContext(sourceScenario),
            ToRunSummaryContext(summary),
            ToParameterValues(selectedParameters),
            (observedFailures ?? [])
                .OrderBy(failure => failure.Model, StringComparer.OrdinalIgnoreCase)
                .ThenBy(failure => failure.Metric, StringComparer.OrdinalIgnoreCase)
                .ThenBy(failure => failure.CollapseTick)
                .Select(ToCollapseContext)
                .ToArray(),
            stressResult is null ? null : ToStressContext(stressResult));

        return new AiAnalysisRequest(
            AiAnalysisKind.ScenarioSuggestion,
            JsonSerializer.Serialize(context, JsonOptions),
            new AiAnalysisProvenance(
                runId is null ? [] : [runId.Value],
                NormalizeStrings(sourceFiles),
                [sourceScenario.Name],
                modelNames,
                [summary.Seed],
                metricNames,
                promptTemplateVersion,
                null,
                null,
                null));
    }

    private static IReadOnlyList<string> ValidationRules =>
    [
        "Scenario name is required.",
        "Scenario ticks must be greater than zero.",
        "Initial population cannot be negative.",
        "Initial resources cannot be negative.",
        "Shock tick must be within 0 <= tick < ticks.",
        "Shock type is required.",
        "Shock severity must be between 0 and 1."
    ];

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

    private static ScenarioContext ToScenarioContext(ScenarioDefinition scenario)
    {
        return new ScenarioContext(
            scenario.Name,
            scenario.Seed,
            scenario.Ticks,
            scenario.InitialPopulation,
            new ResourceContext(
                scenario.InitialResources.Food,
                scenario.InitialResources.Medicine,
                scenario.InitialResources.Housing,
                scenario.InitialResources.AdminCapacity,
                scenario.InitialResources.ProductionCapacity),
            scenario.Shocks
                .OrderBy(shock => shock.Tick)
                .ThenBy(shock => shock.Type, StringComparer.OrdinalIgnoreCase)
                .Select(shock => new ShockContext(
                    shock.Tick,
                    shock.Type,
                    shock.Severity,
                    shock.Parameters.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray()))
                .ToArray());
    }

    private static RunSummaryContext ToRunSummaryContext(SimulationRunSummary summary)
    {
        return new RunSummaryContext(
            summary.ScenarioName,
            summary.Seed,
            summary.Ticks,
            summary.Models
                .OrderBy(model => model.ModelName, StringComparer.OrdinalIgnoreCase)
                .Select(model => new ModelSummaryContext(
                    model.ModelName,
                    model.ModelVersion,
                    model.FinalMetrics
                        .OrderBy(metric => metric.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(metric => new MetricContext(metric.Name, metric.Tick, metric.Value, metric.Unit))
                        .ToArray(),
                    model.NotableMetricChanges
                        .OrderBy(change => change.Metric, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(change => change.ToTick)
                        .Select(change => change.Breadcrumb)
                        .ToArray()))
                .ToArray());
    }

    private static IReadOnlyList<ParameterValueContext> ToParameterValues(IReadOnlyDictionary<string, double>? parameters)
    {
        return parameters is null
            ? []
            : parameters
                .OrderBy(parameter => parameter.Key, StringComparer.OrdinalIgnoreCase)
                .Select(parameter => new ParameterValueContext(parameter.Key, parameter.Value))
                .ToArray();
    }

    private static CollapseContext ToCollapseContext(CollapseEvent failure)
    {
        return new CollapseContext(
            failure.Model,
            failure.Metric,
            failure.Criterion,
            failure.CollapseTick,
            failure.CollapseValue,
            failure.RecoveryTick,
            failure.RecoveryStatus);
    }

    private static StressContext ToStressContext(StressSweepResult result)
    {
        return new StressContext(
            result.GridName,
            result.RunCount,
            result.Scenarios.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            result.Models.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            result.Sensitivity.Metrics
                .OrderBy(metric => metric.ScenarioName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(metric => metric.Model, StringComparer.OrdinalIgnoreCase)
                .ThenBy(metric => metric.Metric, StringComparer.OrdinalIgnoreCase)
                .Select(metric => new SensitivityMetricContext(
                    metric.ScenarioName,
                    metric.Model,
                    metric.Metric,
                    metric.Parameters
                        .OrderBy(parameter => parameter.Parameter, StringComparer.OrdinalIgnoreCase)
                        .Select(parameter => new ParameterSensitivityContext(
                            parameter.Parameter,
                            parameter.OutcomeRange,
                            parameter.Direction,
                            parameter.CorrelationScore))
                        .ToArray()))
                .ToArray(),
            result.ModelRobustness
                .OrderBy(summary => summary.Model, StringComparer.OrdinalIgnoreCase)
                .Select(summary => new RobustnessContext(
                    summary.Model,
                    summary.CollapseRate,
                    summary.WorstAffectedMetric,
                    summary.MostSensitiveParameter,
                    summary.WorstScenarioName))
                .ToArray());
    }

    private sealed record ScenarioSuggestionContext(
        string SourceType,
        string BoundaryRule,
        IReadOnlyList<string> DraftRules,
        IReadOnlyList<string> ValidationRules,
        ScenarioContext SourceScenario,
        RunSummaryContext RunSummary,
        IReadOnlyList<ParameterValueContext> SelectedParameters,
        IReadOnlyList<CollapseContext> ObservedFailures,
        StressContext? StressResults);

    private sealed record ScenarioContext(
        string Name,
        int Seed,
        int Ticks,
        int InitialPopulation,
        ResourceContext InitialResources,
        IReadOnlyList<ShockContext> Shocks);

    private sealed record ResourceContext(
        int Food,
        int Medicine,
        int Housing,
        int AdminCapacity,
        int ProductionCapacity);

    private sealed record ShockContext(
        int Tick,
        string Type,
        double Severity,
        IReadOnlyList<string> ParameterKeys);

    private sealed record RunSummaryContext(
        string ScenarioName,
        int Seed,
        int Ticks,
        IReadOnlyList<ModelSummaryContext> Models);

    private sealed record ModelSummaryContext(
        string ModelName,
        string ModelVersion,
        IReadOnlyList<MetricContext> FinalMetrics,
        IReadOnlyList<string> NotableMetricChangeBreadcrumbs);

    private sealed record MetricContext(string Name, int Tick, double Value, string Unit);

    private sealed record ParameterValueContext(string Name, double Value);

    private sealed record CollapseContext(
        string Model,
        string Metric,
        string Criterion,
        int? CollapseTick,
        double? CollapseValue,
        int? RecoveryTick,
        string RecoveryStatus);

    private sealed record StressContext(
        string? GridName,
        int RunCount,
        IReadOnlyList<string> Scenarios,
        IReadOnlyList<string> Models,
        IReadOnlyList<SensitivityMetricContext> Sensitivity,
        IReadOnlyList<RobustnessContext> ModelRobustness);

    private sealed record SensitivityMetricContext(
        string ScenarioName,
        string Model,
        string Metric,
        IReadOnlyList<ParameterSensitivityContext> Parameters);

    private sealed record ParameterSensitivityContext(
        string Parameter,
        double OutcomeRange,
        string Direction,
        double? CorrelationScore);

    private sealed record RobustnessContext(
        string Model,
        double CollapseRate,
        string? WorstAffectedMetric,
        string? MostSensitiveParameter,
        string? WorstScenarioName);
}

public sealed record AiScenarioSuggestionDraft(
    ScenarioDefinition Scenario,
    IReadOnlyList<string> SuggestedChanges,
    string Rationale,
    bool IsDraft = true);

public sealed record AiScenarioSuggestionValidation(
    bool IsValid,
    IReadOnlyList<string> Errors);

public sealed record AiScenarioSuggestionArtifact(
    AiAnalysisArtifact Analysis,
    AiScenarioSuggestionDraft? Draft,
    AiScenarioSuggestionValidation Validation)
{
    public bool CanSave => Draft is not null && Validation.IsValid;
}

public static class AiScenarioSuggestionDraftReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AiScenarioSuggestionDraft? ReadDraft(AiAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.SuggestedArtifact switch
        {
            null => null,
            AiScenarioSuggestionDraft draft => draft,
            ScenarioDefinition scenario => new AiScenarioSuggestionDraft(
                scenario,
                [],
                "Provider returned a scenario draft without rationale."),
            JsonElement json => ReadJsonElement(json),
            _ => null
        };
    }

    private static AiScenarioSuggestionDraft? ReadJsonElement(JsonElement json)
    {
        try
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (json.TryGetProperty("scenario", out _))
            {
                return json.Deserialize<AiScenarioSuggestionDraft>(JsonOptions);
            }

            var scenario = json.Deserialize<ScenarioDefinition>(JsonOptions);
            return scenario is null
                ? null
                : new AiScenarioSuggestionDraft(
                    scenario,
                    [],
                    "Provider returned a scenario draft without rationale.");
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
