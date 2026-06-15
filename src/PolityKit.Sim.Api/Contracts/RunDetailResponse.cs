using PolityKit.Sim.Analysis;

namespace PolityKit.Sim.Api.Contracts;

public sealed class RunDetailResponse
{
    public Guid Id { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string ScenarioName { get; init; } = "";

    public int Seed { get; init; }

    public int Ticks { get; init; }

    public IReadOnlyList<ModelRunSummaryResponse> Models { get; init; } = [];

    public AiAnalysisUsage AiAnalysis { get; init; } = AiAnalysisUsage.NotUsed();
}
