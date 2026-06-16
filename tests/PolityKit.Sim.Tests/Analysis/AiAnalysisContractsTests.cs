using System.Text.Json;
using System.Text.Json.Serialization;
using PolityKit.Sim.Analysis;

namespace PolityKit.Sim.Tests.Analysis;

public sealed class AiAnalysisContractsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void ArtifactRecordsResultProvenanceAndUsageShape()
    {
        var runId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var createdAt = DateTimeOffset.Parse("2026-06-16T00:00:00Z");
        var provenance = new AiAnalysisProvenance(
            [runId],
            ["runs/example/summary.json"],
            ["Civic Baseline"],
            ["TrustModel"],
            [42],
            ["Trust", "Needs Met"],
            "run-summary-v1",
            "fake-provider",
            "fake-model",
            createdAt);
        var result = new AiAnalysisResult(
            AiAnalysisStatus.Succeeded,
            "Trust improved while needs stayed stable.",
            [
                new AiAnalysisFinding(
                    "Trust movement",
                    "Trust ended higher than the starting value.",
                    AiAnalysisFindingSeverity.Info,
                    0.8,
                    ["Trust final value"],
                    [runId],
                    ["Trust"])
            ],
            ["Review this as advisory interpretation."],
            ["dotnet run --project src/PolityKit.Sim.Cli -- run --scenario examples/scenarios/civic-baseline.json"]);

        var artifact = AiAnalysisArtifact.Create(AiAnalysisKind.RunSummary, result, provenance);

        Assert.Equal(AiAnalysisKind.RunSummary, artifact.Kind);
        Assert.True(artifact.AiAnalysis.Used);
        Assert.Equal([runId], artifact.AiAnalysis.InputRunIds);
        Assert.Equal(["runs/example/summary.json"], artifact.AiAnalysis.InputFiles);
        Assert.Equal(["Civic Baseline"], artifact.AiAnalysis.ScenarioNames);
        Assert.Equal(["TrustModel"], artifact.AiAnalysis.ModelNames);
        Assert.Equal([42], artifact.AiAnalysis.Seeds);
        Assert.Equal(["Trust", "Needs Met"], artifact.AiAnalysis.MetricNames);
        Assert.Equal("run-summary-v1", artifact.AiAnalysis.PromptTemplateVersion);
        Assert.Equal("fake-provider", artifact.AiAnalysis.ProviderName);
        Assert.Equal("fake-model", artifact.AiAnalysis.ProviderModel);
        Assert.Equal(createdAt, artifact.AiAnalysis.CreatedAt);
        Assert.Equal(AiAnalysisUsage.AdvisoryOutputRule, artifact.AiAnalysis.BoundaryRule);
    }

    [Fact]
    public void ContractsRoundTripWithStringEnumsAndCamelCaseProperties()
    {
        var artifact = AiAnalysisArtifact.Create(
            AiAnalysisKind.ModelCritique,
            AiAnalysisResult.Disabled("AI analysis is not configured."),
            new AiAnalysisProvenance(
                [],
                [],
                ["Composite Governance"],
                ["ModelA"],
                [],
                ["Administrative Load"],
                "model-critique-v1",
                null,
                null,
                null));

        var json = JsonSerializer.Serialize(artifact, JsonOptions);
        var restored = JsonSerializer.Deserialize<AiAnalysisArtifact>(json, JsonOptions);

        Assert.Contains("\"kind\":\"ModelCritique\"", json);
        Assert.Contains("\"status\":\"Disabled\"", json);
        Assert.Contains("\"scenarioNames\"", json);
        Assert.DoesNotContain("\"Kind\"", json);
        Assert.NotNull(restored);
        Assert.Equal(AiAnalysisKind.ModelCritique, restored.Kind);
        Assert.Equal(AiAnalysisStatus.Disabled, restored.Result.Status);
        Assert.False(restored.AiAnalysis.Used);
        Assert.Equal(["Composite Governance"], restored.Provenance.ScenarioNames);
        Assert.Equal(["Administrative Load"], restored.AiAnalysis.MetricNames);
        Assert.Equal(AiAnalysisUsage.AdvisoryOutputRule, restored.AiAnalysis.BoundaryRule);
    }

    [Fact]
    public void FailedAnalysisStillRecordsUsageWhenProviderMayHaveReadInputs()
    {
        var runId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var artifact = AiAnalysisArtifact.Create(
            AiAnalysisKind.BatchAnomalyReport,
            AiAnalysisResult.Failed("Provider timed out."),
            new AiAnalysisProvenance(
                [runId],
                ["runs/batch/stress-summary.json"],
                [],
                [],
                [],
                [],
                "batch-anomaly-v1",
                "fake-provider",
                "fake-model",
                DateTimeOffset.Parse("2026-06-16T00:10:00Z")));

        Assert.True(artifact.AiAnalysis.Used);
        Assert.Equal("fake-provider", artifact.AiAnalysis.ProviderName);
        Assert.Equal([runId], artifact.AiAnalysis.InputRunIds);
        Assert.Equal(["Provider timed out."], artifact.Result.Warnings);
    }
}
