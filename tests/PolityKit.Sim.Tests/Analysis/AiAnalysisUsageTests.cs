using System.Text.Json;
using System.Text.Json.Serialization;
using PolityKit.Sim.Analysis;

namespace PolityKit.Sim.Tests.Analysis;

public sealed class AiAnalysisUsageTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void NotUsedRecordsThatAiAnalysisWasNotRequired()
    {
        var usage = AiAnalysisUsage.NotUsed();

        Assert.False(usage.Used);
        Assert.Empty(usage.InputRunIds);
        Assert.Empty(usage.InputFiles);
        Assert.Empty(usage.ScenarioNames);
        Assert.Empty(usage.ModelNames);
        Assert.Empty(usage.Seeds);
        Assert.Empty(usage.MetricNames);
        Assert.Null(usage.ProviderName);
        Assert.Null(usage.ProviderModel);
        Assert.Null(usage.PromptTemplateVersion);
        Assert.Null(usage.CreatedAt);
        Assert.Equal(AiAnalysisUsage.AdvisoryOutputRule, usage.BoundaryRule);
    }

    [Fact]
    public void UsageShapeRecordsInputsProviderModelAndCreationTime()
    {
        var runId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var createdAt = DateTimeOffset.Parse("2026-06-15T00:00:00Z");

        var usage = new AiAnalysisUsage
        {
            Used = true,
            InputRunIds = [runId],
            InputFiles = ["runs/example/summary.json"],
            ScenarioNames = ["Civic Baseline"],
            ModelNames = ["TrustModel"],
            Seeds = [12345],
            MetricNames = ["Trust"],
            ProviderName = "example-provider",
            ProviderModel = "example-model",
            PromptTemplateVersion = "run-summary-v1",
            CreatedAt = createdAt
        };

        var json = JsonSerializer.Serialize(usage, JsonOptions);
        var restored = JsonSerializer.Deserialize<AiAnalysisUsage>(json, JsonOptions);

        Assert.NotNull(restored);
        Assert.True(restored.Used);
        Assert.Equal([runId], restored.InputRunIds);
        Assert.Equal(["runs/example/summary.json"], restored.InputFiles);
        Assert.Equal(["Civic Baseline"], restored.ScenarioNames);
        Assert.Equal(["TrustModel"], restored.ModelNames);
        Assert.Equal([12345], restored.Seeds);
        Assert.Equal(["Trust"], restored.MetricNames);
        Assert.Equal("example-provider", restored.ProviderName);
        Assert.Equal("example-model", restored.ProviderModel);
        Assert.Equal("run-summary-v1", restored.PromptTemplateVersion);
        Assert.Equal(createdAt, restored.CreatedAt);
        Assert.Equal(AiAnalysisUsage.AdvisoryOutputRule, restored.BoundaryRule);
    }
}
