namespace PolityKit.Sim.Analysis;

public sealed class AiAnalysisUsage
{
    public const string AdvisoryOutputRule =
        "AI output is advisory text or proposed artifacts, not authoritative simulation data.";

    public bool Used { get; init; }

    public IReadOnlyList<Guid> InputRunIds { get; init; } = [];

    public IReadOnlyList<string> InputFiles { get; init; } = [];

    public IReadOnlyList<string> ScenarioNames { get; init; } = [];

    public IReadOnlyList<string> ModelNames { get; init; } = [];

    public IReadOnlyList<int> Seeds { get; init; } = [];

    public IReadOnlyList<string> MetricNames { get; init; } = [];

    public string? ProviderName { get; init; }

    public string? ProviderModel { get; init; }

    public string? PromptTemplateVersion { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public string BoundaryRule { get; init; } = AdvisoryOutputRule;

    public static AiAnalysisUsage NotUsed() => new();
}
