using System.Text.Json.Serialization;

namespace PolityKit.Sim.Analysis;

[JsonConverter(typeof(JsonStringEnumConverter<AiAnalysisKind>))]
public enum AiAnalysisKind
{
    RunSummary,
    ScenarioSuggestion,
    ModelCritique,
    BatchAnomalyReport
}

[JsonConverter(typeof(JsonStringEnumConverter<AiAnalysisStatus>))]
public enum AiAnalysisStatus
{
    Succeeded,
    Disabled,
    Failed
}

[JsonConverter(typeof(JsonStringEnumConverter<AiAnalysisFindingSeverity>))]
public enum AiAnalysisFindingSeverity
{
    Info,
    Warning,
    Critical
}

public sealed record AiAnalysisRequest(
    AiAnalysisKind Kind,
    string Context,
    AiAnalysisProvenance Provenance,
    IReadOnlyDictionary<string, string>? Options = null);

public sealed record AiAnalysisResult(
    AiAnalysisStatus Status,
    string GeneratedText,
    IReadOnlyList<AiAnalysisFinding> Findings,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> SuggestedFollowUpCommands,
    object? SuggestedArtifact = null)
{
    public static AiAnalysisResult Disabled(string message) => new(
        AiAnalysisStatus.Disabled,
        message,
        [],
        [message],
        []);

    public static AiAnalysisResult Failed(string message) => new(
        AiAnalysisStatus.Failed,
        "",
        [],
        [message],
        []);
}

public sealed record AiAnalysisArtifact(
    AiAnalysisKind Kind,
    AiAnalysisResult Result,
    AiAnalysisProvenance Provenance,
    AiAnalysisUsage AiAnalysis)
{
    public static AiAnalysisArtifact Create(
        AiAnalysisKind kind,
        AiAnalysisResult result,
        AiAnalysisProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(provenance);

        return new AiAnalysisArtifact(kind, result, provenance, provenance.ToUsage(result.Status != AiAnalysisStatus.Disabled));
    }

    public static AiAnalysisArtifact Create(
        AiAnalysisKind kind,
        AiAnalysisResult result,
        AiAnalysisProvenance provenance,
        bool used)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(provenance);

        return new AiAnalysisArtifact(kind, result, provenance, provenance.ToUsage(used));
    }
}

public sealed record AiAnalysisProvenance(
    IReadOnlyList<Guid> SourceRunIds,
    IReadOnlyList<string> SourceFiles,
    IReadOnlyList<string> ScenarioNames,
    IReadOnlyList<string> ModelNames,
    IReadOnlyList<int> Seeds,
    IReadOnlyList<string> MetricNames,
    string? PromptTemplateVersion,
    string? ProviderName,
    string? ProviderModel,
    DateTimeOffset? CreatedAt)
{
    public AiAnalysisUsage ToUsage(bool used) => new()
    {
        Used = used,
        InputRunIds = SourceRunIds,
        InputFiles = SourceFiles,
        ScenarioNames = ScenarioNames,
        ModelNames = ModelNames,
        Seeds = Seeds,
        MetricNames = MetricNames,
        ProviderName = ProviderName,
        ProviderModel = ProviderModel,
        PromptTemplateVersion = PromptTemplateVersion,
        CreatedAt = CreatedAt
    };
}

public sealed record AiAnalysisFinding(
    string Title,
    string Summary,
    AiAnalysisFindingSeverity Severity,
    double? Confidence,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<Guid> SourceRunIds,
    IReadOnlyList<string> RelatedMetricNames);
