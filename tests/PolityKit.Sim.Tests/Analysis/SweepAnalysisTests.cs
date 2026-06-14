using PolityKit.Sim.Analysis;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Tests.Analysis;

public sealed class SweepAnalysisTests
{
    [Fact]
    public void BuildParameterCombinationsUsesSortedParameterOrderAndBaseOverrides()
    {
        var combinations = SweepAnalysis.BuildParameterCombinations(
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["fixedWeight"] = 10,
                ["NeedPriorityWeight"] = 0.5
            },
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["vulnerabilityPriorityWeight"] = [0.25, 0.5],
                ["needPriorityWeight"] = [1.0, 2.0]
            });

        Assert.Equal(4, combinations.Count);
        Assert.Equal(
            [(1.0, 0.25), (1.0, 0.5), (2.0, 0.25), (2.0, 0.5)],
            combinations.Select(parameters => (
                Need: parameters["needPriorityWeight"],
                Vulnerability: parameters["vulnerabilityPriorityWeight"])).ToArray());
        Assert.All(combinations, parameters => Assert.Equal(10, parameters["fixedWeight"]));
    }

    [Fact]
    public void NormalizeSweepRejectsRunCountsOverLimit()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SweepAnalysis.NormalizeSweep(
                new Dictionary<string, IReadOnlyList<double>>
                {
                    ["a"] = Enumerable.Range(0, 17).Select(value => (double)value).ToArray(),
                    ["b"] = Enumerable.Range(0, 16).Select(value => (double)value).ToArray()
                }));

        Assert.Equal("Sweep would create 272 runs; the maximum is 256.", exception.Message);
    }

    [Fact]
    public void SelectFinalMetricsReturnsLatestTickPerModelMetric()
    {
        var result = new SimulationRunResult
        {
            ModelResults =
            [
                new ModelRunResult
                {
                    ModelName = "ModelA",
                    Metrics =
                    [
                        Metric("Trust", 0, 10, "points"),
                        Metric("Trust", 2, 40, "points"),
                        Metric("Needs Met", 1, 0.75, "ratio")
                    ]
                }
            ]
        };

        var metrics = SweepAnalysis.SelectFinalMetrics(result);

        Assert.Equal(
            [("ModelA", "Needs Met", 1, 0.75), ("ModelA", "Trust", 2, 40)],
            metrics.Select(metric => (metric.Model, metric.Name, metric.Tick, metric.Value)).ToArray());
    }

    [Fact]
    public void BuildBestWorstUsesMetricDirection()
    {
        var runs = new[]
        {
            Run(1, 0.9, 4),
            Run(2, 0.5, 8)
        };

        var bestWorst = SweepAnalysis.BuildBestWorst(runs);

        var needsMet = Assert.Single(bestWorst, report => report.Metric == "Needs Met");
        Assert.Equal("higher", needsMet.BestDirection);
        Assert.Equal(1, needsMet.Best.RunIndex);
        Assert.Equal(2, needsMet.Worst.RunIndex);

        var severeFailures = Assert.Single(bestWorst, report => report.Metric == "Severe Failures");
        Assert.Equal("lower", severeFailures.BestDirection);
        Assert.Equal(1, severeFailures.Best.RunIndex);
        Assert.Equal(2, severeFailures.Worst.RunIndex);
    }

    private static SweepRunReport Run(int index, double needsMet, double severeFailures)
    {
        return new SweepRunReport(
            index,
            $"run-{index:000}",
            new Dictionary<string, double> { ["weight"] = index },
            [
                new SweepMetricReport("ModelA", 2, "Needs Met", needsMet, "ratio"),
                new SweepMetricReport("ModelA", 2, "Severe Failures", severeFailures, "citizens")
            ]);
    }

    private static MetricResult Metric(string name, int tick, double value, string unit)
    {
        return new MetricResult
        {
            Name = name,
            Tick = tick,
            Value = value,
            Unit = unit
        };
    }
}
