using System.Text.Json;
using PolityKit.Sim.Analysis;
using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Tests.Analysis;

public sealed class AiAnalysisContextBuildersTests
{
    [Fact]
    public void BuildRunSummaryRequestIncludesReviewableDeterministicContext()
    {
        var runId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var result = RunResult(
            models:
            [
                ModelResult(
                    "ModelB",
                    "2.0",
                    [
                        new MetricResult { Name = "Trust", Tick = 0, Value = 80, Unit = "points" },
                        new MetricResult { Name = "Trust", Tick = 2, Value = 70, Unit = "points" }
                    ],
                    []),
                ModelResult(
                    "ModelA",
                    "1.0",
                    [
                        new MetricResult { Name = "Needs Met", Tick = 0, Value = 0.95, Unit = "ratio" },
                        new MetricResult { Name = "Needs Met", Tick = 2, Value = 0.60, Unit = "ratio" }
                    ],
                    [
                        new SimulationEvent
                        {
                            Tick = 1,
                            Type = "CropFailure",
                            Description = "Food production fell.",
                            Data = { ["privateNote"] = "do not send", ["severity"] = 0.4 }
                        }
                    ])
            ]);

        var request = AiAnalysisContextBuilders.BuildRunSummaryRequest(
            result,
            new Dictionary<string, double>
            {
                ["zetaWeight"] = 2,
                ["alphaWeight"] = 1
            },
            ["Assumption Z", "Assumption A"],
            runId,
            ["runs/example/summary.json"]);

        using var document = JsonDocument.Parse(request.Context);
        var root = document.RootElement;

        Assert.Equal(AiAnalysisKind.RunSummary, request.Kind);
        Assert.Equal(AiAnalysisContextBuilders.RunSummaryPromptTemplateVersion, request.Provenance.PromptTemplateVersion);
        Assert.Equal([runId], request.Provenance.SourceRunIds);
        Assert.Equal(["runs/example/summary.json"], request.Provenance.SourceFiles);
        Assert.Equal(["Scenario A"], request.Provenance.ScenarioNames);
        Assert.Equal(["ModelA", "ModelB"], request.Provenance.ModelNames);
        Assert.Equal([123], request.Provenance.Seeds);
        Assert.Equal(["Needs Met", "Trust"], request.Provenance.MetricNames);
        Assert.Equal("single-run", root.GetProperty("sourceType").GetString());
        Assert.Equal("Scenario A", root.GetProperty("scenarioName").GetString());
        Assert.Equal(123, root.GetProperty("seed").GetInt32());
        Assert.Equal(
            ["alphaWeight", "zetaWeight"],
            root.GetProperty("selectedParameters").EnumerateArray()
                .Select(parameter => parameter.GetProperty("name").GetString()!)
                .ToArray());
        Assert.Equal(
            ["Assumption A", "Assumption Z"],
            root.GetProperty("relevantAssumptions").EnumerateArray()
                .Select(assumption => assumption.GetString()!)
                .ToArray());
        Assert.Equal(
            ["ModelA", "ModelB"],
            root.GetProperty("models").EnumerateArray()
                .Select(model => model.GetProperty("modelName").GetString()!)
                .ToArray());

        var firstModel = root.GetProperty("models")[0];
        Assert.Equal("1.0", firstModel.GetProperty("modelVersion").GetString());
        var change = Assert.Single(firstModel.GetProperty("notableMetricChanges").EnumerateArray());
        Assert.Equal("Needs Met", change.GetProperty("metric").GetString());
        var nearbyEvent = Assert.Single(change.GetProperty("nearbyEvents").EnumerateArray());
        Assert.Equal("CropFailure", nearbyEvent.GetProperty("type").GetString());
        Assert.Equal(["privateNote", "severity"], nearbyEvent.GetProperty("dataKeys").EnumerateArray()
            .Select(key => key.GetString()!)
            .ToArray());
        Assert.DoesNotContain("do not send", request.Context);
        Assert.Contains("Raw citizen state is excluded.", request.Context);
    }

    [Fact]
    public void BuildComparisonRequestSortsMetricDeltasAndRecordsProvenance()
    {
        var baselineId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var comparisonId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var request = AiAnalysisContextBuilders.BuildComparisonRequest(
            new AiRunComparisonContext(
                new AiRunReference(baselineId, null, "Baseline", 222, 10, ["ModelB", "ModelA"]),
                new AiRunReference(comparisonId, null, "Shock", 111, 10, ["ModelA"]),
                [
                    new AiMetricDeltaContext("ModelB", "Trust", "points", 9, 9, 80, 70, -10, -0.125, "decreased"),
                    new AiMetricDeltaContext("ModelA", "Needs Met", "ratio", 9, 9, 0.8, 0.6, -0.2, -0.25, "decreased")
                ]));

        using var document = JsonDocument.Parse(request.Context);
        var root = document.RootElement;

        Assert.Equal(AiAnalysisKind.RunSummary, request.Kind);
        Assert.Equal([baselineId, comparisonId], request.Provenance.SourceRunIds);
        Assert.Equal(["Baseline", "Shock"], request.Provenance.ScenarioNames);
        Assert.Equal(["ModelA", "ModelB"], request.Provenance.ModelNames);
        Assert.Equal([111, 222], request.Provenance.Seeds);
        Assert.Equal(["Needs Met", "Trust"], request.Provenance.MetricNames);
        Assert.Equal("run-comparison", root.GetProperty("sourceType").GetString());
        Assert.Equal(
            ["ModelA:Needs Met", "ModelB:Trust"],
            root.GetProperty("metricDeltas").EnumerateArray()
                .Select(delta => $"{delta.GetProperty("model").GetString()}:{delta.GetProperty("metric").GetString()}")
                .ToArray());
        Assert.Equal(
            ["ModelA", "ModelB"],
            root.GetProperty("baseline").GetProperty("models").EnumerateArray()
                .Select(model => model.GetString()!)
                .ToArray());
    }

    [Fact]
    public void BuildStressSummaryRequestIncludesStressDiagnosticsAndStableOrdering()
    {
        var lowRunId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var highRunId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var collapse = new CollapseEvent(
            "ModelA",
            "Needs Met",
            "Needs Met",
            0.5,
            FailureOperator.LessThan,
            3,
            0.4,
            null,
            null,
            "no recovery observed",
            []);
        var runs = new[]
        {
            StressRun(2, highRunId, "Crisis", 222, 2, 0.4, [collapse]),
            StressRun(1, lowRunId, "Stable", 111, 1, 0.9, [])
        };
        var sensitivity = SensitivityAnalysis.BuildReport(runs, new Dictionary<string, double> { ["stressLevel"] = 1 });
        var result = new StressSweepResult(
            "grid-a",
            ["Stable", "Crisis"],
            [222, 111],
            ["ModelA"],
            new Dictionary<string, double> { ["stressLevel"] = 1 },
            new Dictionary<string, IReadOnlyList<double>> { ["stressLevel"] = [2, 1] },
            2,
            runs,
            SweepAnalysis.BuildBestWorst(runs.Select(run => new SweepRunReport(
                run.RunIndex,
                run.Directory,
                run.Parameters,
                run.FinalMetrics)).ToArray()),
            [collapse],
            sensitivity,
            RobustnessAnalysis.BuildModelSummaries(runs, sensitivity));

        var request = AiAnalysisContextBuilders.BuildStressSummaryRequest(result);

        using var document = JsonDocument.Parse(request.Context);
        var root = document.RootElement;

        Assert.Equal(AiAnalysisKind.BatchAnomalyReport, request.Kind);
        Assert.Equal([lowRunId, highRunId], request.Provenance.SourceRunIds);
        Assert.Equal(["Crisis", "Stable"], request.Provenance.ScenarioNames);
        Assert.Equal(["ModelA"], request.Provenance.ModelNames);
        Assert.Equal([111, 222], request.Provenance.Seeds);
        Assert.Equal(["Needs Met"], request.Provenance.MetricNames);
        Assert.Equal("stress-summary", root.GetProperty("sourceType").GetString());
        Assert.Equal(
            [111, 222],
            root.GetProperty("seeds").EnumerateArray().Select(seed => seed.GetInt32()).ToArray());
        Assert.Equal(
            [1, 2],
            root.GetProperty("sweep")[0].GetProperty("values").EnumerateArray()
                .Select(value => value.GetDouble())
                .ToArray());
        Assert.Equal(
            [1, 2],
            root.GetProperty("runs").EnumerateArray()
                .Select(run => run.GetProperty("runIndex").GetInt32())
                .ToArray());
        Assert.NotEmpty(root.GetProperty("collapseEvents").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("sensitivity").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("modelRobustness").EnumerateArray());
        Assert.Contains("Full event streams are excluded", request.Context);
    }

    [Fact]
    public void BuildRunSummaryRequestProducesSameContextForEquivalentUnorderedInputs()
    {
        var left = RunResult(
            models:
            [
                ModelResult("ModelB", "1.0", [new MetricResult { Name = "Trust", Tick = 2, Value = 70, Unit = "points" }], []),
                ModelResult("ModelA", "1.0", [new MetricResult { Name = "Needs Met", Tick = 2, Value = 0.8, Unit = "ratio" }], [])
            ]);
        var right = RunResult(
            models:
            [
                ModelResult("ModelA", "1.0", [new MetricResult { Name = "Needs Met", Tick = 2, Value = 0.8, Unit = "ratio" }], []),
                ModelResult("ModelB", "1.0", [new MetricResult { Name = "Trust", Tick = 2, Value = 70, Unit = "points" }], [])
            ]);

        var leftRequest = AiAnalysisContextBuilders.BuildRunSummaryRequest(
            left,
            new Dictionary<string, double> { ["b"] = 2, ["a"] = 1 },
            ["Z", "A"]);
        var rightRequest = AiAnalysisContextBuilders.BuildRunSummaryRequest(
            right,
            new Dictionary<string, double> { ["a"] = 1, ["b"] = 2 },
            ["A", "Z"]);

        Assert.Equal(leftRequest.Context, rightRequest.Context);
    }

    private static SimulationRunResult RunResult(IReadOnlyList<ModelRunResult> models)
    {
        return new SimulationRunResult
        {
            ScenarioName = "Scenario A",
            Seed = 123,
            Ticks = 3,
            ModelResults = models
        };
    }

    private static ModelRunResult ModelResult(
        string name,
        string version,
        IReadOnlyList<MetricResult> metrics,
        IReadOnlyList<SimulationEvent> events)
    {
        return new ModelRunResult
        {
            ModelName = name,
            ModelVersion = version,
            Metrics = metrics,
            Events = events
        };
    }

    private static StressSweepRunResult StressRun(
        int index,
        Guid runId,
        string scenario,
        int seed,
        double stressLevel,
        double needsMet,
        IReadOnlyList<CollapseEvent> collapseEvents)
    {
        return new StressSweepRunResult(
            index,
            $"run-{index:000}",
            runId,
            scenario,
            seed,
            10,
            "ModelA",
            new Dictionary<string, double> { ["stressLevel"] = stressLevel },
            [new SweepMetricReport("ModelA", 9, "Needs Met", needsMet, "ratio")],
            collapseEvents);
    }
}
