using PolityKit.Sim.Analysis;

namespace PolityKit.Sim.Tests.Analysis;

public sealed class RobustnessAnalysisTests
{
    [Fact]
    public void BuildModelSummariesAggregatesCollapseRecoveryAndScenarioOutcomes()
    {
        var runs = new[]
        {
            Run(1, "Stable", 111, 1, 0.9, []),
            Run(2, "Stable", 222, 1, 0.8, []),
            Run(3, "Crisis", 111, 2, 0.4, [Collapse("Needs Met", 3, recovered: true)]),
            Run(4, "Crisis", 222, 2, 0.3, [Collapse("Trust", 5, recovered: false)])
        };
        var sensitivity = SensitivityAnalysis.BuildReport(runs);

        var summaries = RobustnessAnalysis.BuildModelSummaries(runs, sensitivity);

        var summary = Assert.Single(summaries);
        Assert.Equal("ModelA", summary.Model);
        Assert.Equal(["Crisis", "Stable"], summary.ScenariosTested);
        Assert.Equal([111, 222], summary.SeedsTested);
        Assert.Equal(4, summary.RunsCompleted);
        Assert.Equal(0.5, summary.CollapseRate);
        Assert.Equal(4, summary.MedianCollapseTick);
        Assert.Equal(3, summary.EarliestCollapseTick);
        Assert.Equal(0.5, summary.RecoveryRate);
        Assert.Equal("Needs Met", summary.WorstAffectedMetric);
        Assert.Equal("stressLevel", summary.MostSensitiveParameter);
        Assert.Equal("Stable", summary.BestScenarioName);
        Assert.Equal("Crisis", summary.WorstScenarioName);
    }

    [Fact]
    public void BuildModelSummariesReturnsNullCollapseFieldsWhenModelNeverCollapsed()
    {
        var summaries = RobustnessAnalysis.BuildModelSummaries(
        [
            Run(1, "Stable", 111, 1, 0.9, []),
            Run(2, "Stable", 222, 1, 0.8, [])
        ]);

        var summary = Assert.Single(summaries);
        Assert.Equal(0, summary.CollapseRate);
        Assert.Null(summary.MedianCollapseTick);
        Assert.Null(summary.EarliestCollapseTick);
        Assert.Equal(0, summary.RecoveryRate);
        Assert.Null(summary.WorstAffectedMetric);
    }

    private static StressSweepRunResult Run(
        int index,
        string scenario,
        int seed,
        double stressLevel,
        double needsMet,
        IReadOnlyList<CollapseEvent> collapseEvents)
    {
        return new StressSweepRunResult(
            index,
            null,
            null,
            scenario,
            seed,
            10,
            "ModelA",
            new Dictionary<string, double> { ["stressLevel"] = stressLevel },
            [new SweepMetricReport("ModelA", 9, "Needs Met", needsMet, "ratio")],
            collapseEvents);
    }

    private static CollapseEvent Collapse(string metric, int collapseTick, bool recovered)
    {
        return new CollapseEvent(
            "ModelA",
            metric,
            metric,
            0.5,
            FailureOperator.LessThan,
            collapseTick,
            0.4,
            recovered ? collapseTick + 2 : null,
            recovered ? 0.7 : null,
            recovered ? "recovered" : "no recovery observed",
            []);
    }
}
