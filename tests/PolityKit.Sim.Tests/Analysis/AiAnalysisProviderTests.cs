using PolityKit.Sim.Analysis;

namespace PolityKit.Sim.Tests.Analysis;

public sealed class AiAnalysisProviderTests
{
    [Fact]
    public async Task DisabledModeReturnsClearNotConfiguredArtifactWithoutUsingAi()
    {
        var service = new AiAnalysisService(
            new RecordingProvider(AiAnalysisResult.Failed("Should not be called.")),
            new AiAnalysisOptions { Enabled = false });

        var artifact = await service.AnalyzeAsync(Request());

        Assert.Equal(AiAnalysisStatus.Disabled, artifact.Result.Status);
        Assert.Equal("AI analysis is not configured.", artifact.Result.GeneratedText);
        Assert.False(artifact.AiAnalysis.Used);
        Assert.Null(artifact.AiAnalysis.ProviderName);
    }

    [Fact]
    public async Task InputLimitReturnsDeterministicFailureBeforeCallingProvider()
    {
        var provider = new RecordingProvider(new AiAnalysisResult(
            AiAnalysisStatus.Succeeded,
            "unused",
            [],
            [],
            []));
        var service = new AiAnalysisService(
            provider,
            new AiAnalysisOptions
            {
                Enabled = true,
                MaxInputCharacters = 4
            });

        var artifact = await service.AnalyzeAsync(Request(context: "too long"));

        Assert.Equal(AiAnalysisStatus.Failed, artifact.Result.Status);
        Assert.Equal(["AI analysis input exceeds the configured maximum of 4 characters."], artifact.Result.Warnings);
        Assert.False(artifact.AiAnalysis.Used);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task SuccessfulProviderOutputIsBoundedAndStampedWithProviderMetadata()
    {
        var provider = new RecordingProvider(new AiAnalysisResult(
            AiAnalysisStatus.Succeeded,
            "abcdef",
            [],
            ["provider warning"],
            ["next command"]));
        var service = new AiAnalysisService(
            provider,
            new AiAnalysisOptions
            {
                Enabled = true,
                MaxOutputCharacters = 3
            });

        var artifact = await service.AnalyzeAsync(Request());

        Assert.Equal(AiAnalysisStatus.Succeeded, artifact.Result.Status);
        Assert.Equal("abc", artifact.Result.GeneratedText);
        Assert.Contains("provider warning", artifact.Result.Warnings);
        Assert.Contains("AI analysis output exceeded the configured maximum of 3 characters and was truncated.", artifact.Result.Warnings);
        Assert.True(artifact.AiAnalysis.Used);
        Assert.Equal("fake-provider", artifact.AiAnalysis.ProviderName);
        Assert.Equal("fake-model", artifact.AiAnalysis.ProviderModel);
        Assert.NotNull(artifact.AiAnalysis.CreatedAt);
    }

    [Fact]
    public async Task ProviderFailureReturnsDeterministicFailureWithoutPromptContents()
    {
        var service = new AiAnalysisService(
            new ThrowingProvider(),
            new AiAnalysisOptions { Enabled = true });

        var artifact = await service.AnalyzeAsync(Request(context: "sensitive prompt text"));

        Assert.Equal(AiAnalysisStatus.Failed, artifact.Result.Status);
        Assert.Equal(["AI analysis provider failed with InvalidOperationException."], artifact.Result.Warnings);
        Assert.DoesNotContain("sensitive prompt text", artifact.Result.Warnings.Single());
        Assert.True(artifact.AiAnalysis.Used);
        Assert.Equal("throwing-provider", artifact.AiAnalysis.ProviderName);
    }

    [Fact]
    public async Task TimeoutReturnsDeterministicFailure()
    {
        var service = new AiAnalysisService(
            new SlowProvider(),
            new AiAnalysisOptions
            {
                Enabled = true,
                Timeout = TimeSpan.FromMilliseconds(10)
            });

        var artifact = await service.AnalyzeAsync(Request());

        Assert.Equal(AiAnalysisStatus.Failed, artifact.Result.Status);
        Assert.Equal(["AI analysis provider timed out after 0.01 seconds."], artifact.Result.Warnings);
        Assert.True(artifact.AiAnalysis.Used);
        Assert.Equal("slow-provider", artifact.AiAnalysis.ProviderName);
    }

    [Fact]
    public async Task ExternalCancellationIsPropagated()
    {
        var service = new AiAnalysisService(
            new SlowProvider(),
            new AiAnalysisOptions
            {
                Enabled = true,
                Timeout = TimeSpan.FromSeconds(10)
            });
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.AnalyzeAsync(Request(), cancellation.Token));
    }

    private static AiAnalysisRequest Request(string context = "{}")
    {
        return new AiAnalysisRequest(
            AiAnalysisKind.RunSummary,
            context,
            new AiAnalysisProvenance(
                [Guid.Parse("11111111-1111-1111-1111-111111111111")],
                ["runs/example/summary.json"],
                ["Scenario A"],
                ["ModelA"],
                [123],
                ["Trust"],
                "run-summary-context-v1",
                null,
                null,
                null));
    }

    private sealed class RecordingProvider(AiAnalysisResult result) : IAiAnalysisProvider
    {
        public int CallCount { get; private set; }

        public string ProviderName => "fake-provider";

        public string ProviderModel => "fake-model";

        public Task<AiAnalysisResult> AnalyzeAsync(
            AiAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingProvider : IAiAnalysisProvider
    {
        public string ProviderName => "throwing-provider";

        public string ProviderModel => "fake-model";

        public Task<AiAnalysisResult> AnalyzeAsync(
            AiAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("sensitive provider details");
        }
    }

    private sealed class SlowProvider : IAiAnalysisProvider
    {
        public string ProviderName => "slow-provider";

        public string ProviderModel => "fake-model";

        public async Task<AiAnalysisResult> AnalyzeAsync(
            AiAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return new AiAnalysisResult(AiAnalysisStatus.Succeeded, "done", [], [], []);
        }
    }
}
