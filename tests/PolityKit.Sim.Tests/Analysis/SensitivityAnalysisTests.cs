using PolityKit.Sim.Analysis;

namespace PolityKit.Sim.Tests.Analysis;

public sealed class SensitivityAnalysisTests
{
    [Fact]
    public void BuildReportRanksParametersByOutcomeRange()
    {
        var runs = new[]
        {
            Run(1, 1, 1, 10),
            Run(2, 2, 1, 30),
            Run(3, 1, 2, 12),
            Run(4, 2, 2, 32)
        };

        var report = SensitivityAnalysis.BuildReport(
            "synthetic-scenario",
            runs,
            new Dictionary<string, double>
            {
                ["largeEffect"] = 1,
                ["smallEffect"] = 1
            });

        var metric = Assert.Single(report.Metrics);
        Assert.Equal("synthetic-scenario", metric.ScenarioName);
        Assert.Equal("ModelA", metric.Model);
        Assert.Equal("Trust", metric.Metric);

        Assert.Equal(["largeEffect", "smallEffect"], metric.Parameters.Select(parameter => parameter.Parameter).ToArray());
        var largeEffect = metric.Parameters[0];
        Assert.Equal(20, largeEffect.OutcomeRange);
        Assert.Equal("increases", largeEffect.Direction);
        Assert.Equal(20, largeEffect.DeltaFromBaseline);
        Assert.True(largeEffect.CorrelationScore > 0.99);

        var smallEffect = metric.Parameters[1];
        Assert.Equal(2, smallEffect.OutcomeRange);
        Assert.Equal("increases", smallEffect.Direction);
        Assert.Equal(2, smallEffect.DeltaFromBaseline);
    }

    [Fact]
    public void BuildReportSeparatesScenarioModelAndMetric()
    {
        var runs = new[]
        {
            new StressSweepRunResult(
                1,
                null,
                null,
                "ScenarioA",
                111,
                2,
                "ModelA",
                new Dictionary<string, double> { ["weight"] = 1 },
                [Metric("ModelA", "Needs Met", 0.5)],
                []),
            new StressSweepRunResult(
                2,
                null,
                null,
                "ScenarioB",
                111,
                2,
                "ModelA",
                new Dictionary<string, double> { ["weight"] = 2 },
                [Metric("ModelA", "Needs Met", 0.8)],
                [])
        };

        var report = SensitivityAnalysis.BuildReport(runs);

        Assert.Equal(["ScenarioA", "ScenarioB"], report.Metrics.Select(metric => metric.ScenarioName).ToArray());
        Assert.All(report.Metrics, metric => Assert.Equal("Needs Met", metric.Metric));
    }

    private static SweepRunReport Run(int index, double largeEffect, double smallEffect, double outcome)
    {
        return new SweepRunReport(
            index,
            null,
            new Dictionary<string, double>
            {
                ["largeEffect"] = largeEffect,
                ["smallEffect"] = smallEffect
            },
            [Metric("ModelA", "Trust", outcome)]);
    }

    private static SweepMetricReport Metric(string model, string metric, double value)
    {
        return new SweepMetricReport(model, 1, metric, value, "points");
    }
}
