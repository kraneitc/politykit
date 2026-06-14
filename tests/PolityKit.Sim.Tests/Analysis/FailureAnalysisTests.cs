using PolityKit.Sim.Analysis;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Tests.Analysis;

public sealed class FailureAnalysisTests
{
    [Fact]
    public void DetectCollapsesFindsFirstCrossingAndRecoveryAfterRequiredTicks()
    {
        var result = Result(
            Metric("Needs Met", 0, 0.9),
            Metric("Needs Met", 1, 0.49),
            Metric("Needs Met", 2, 0.6),
            Metric("Needs Met", 3, 0.7));

        var collapses = FailureAnalysis.DetectCollapses(result,
        [
            new FailureCriterion("Needs Met", FailureOperator.LessThan, 0.5, RecoveryTicks: 2)
        ]);

        var collapse = Assert.Single(collapses);
        Assert.True(collapse.Collapsed);
        Assert.True(collapse.Recovered);
        Assert.Equal(1, collapse.CollapseTick);
        Assert.Equal(0.49, collapse.CollapseValue);
        Assert.Equal(3, collapse.RecoveryTick);
        Assert.Equal("recovered", collapse.RecoveryStatus);
    }

    [Fact]
    public void DetectCollapsesHonorsBoundaryOperators()
    {
        var result = Result(Metric("Trust", 0, 25));

        var lessThan = Assert.Single(FailureAnalysis.DetectCollapses(result,
        [
            new FailureCriterion("Trust", FailureOperator.LessThan, 25)
        ]));
        var lessThanOrEqual = Assert.Single(FailureAnalysis.DetectCollapses(result,
        [
            new FailureCriterion("Trust", FailureOperator.LessThanOrEqual, 25)
        ]));

        Assert.False(lessThan.Collapsed);
        Assert.True(lessThanOrEqual.Collapsed);
        Assert.Equal(0, lessThanOrEqual.CollapseTick);
    }

    [Fact]
    public void DetectCollapsesUsesPopulationShareThresholds()
    {
        var result = Result(
            20,
            Metric("Severe Failures", 0, 1),
            Metric("Severe Failures", 1, 2));

        var collapse = Assert.Single(FailureAnalysis.DetectCollapses(result,
        [
            new FailureCriterion(
                "Severe Failures",
                FailureOperator.GreaterThanOrEqual,
                0.10,
                FailureThresholdKind.PopulationShare)
        ]));

        Assert.True(collapse.Collapsed);
        Assert.Equal(2, collapse.Threshold);
        Assert.Equal(1, collapse.CollapseTick);
    }

    [Fact]
    public void DetectCollapsesReturnsNoCollapseObservedWhenMetricNeverFails()
    {
        var result = Result(
            Metric("Administrative Load", 0, 1),
            Metric("Administrative Load", 1, 2));

        var collapse = Assert.Single(FailureAnalysis.DetectCollapses(result,
        [
            new FailureCriterion("Administrative Load", FailureOperator.GreaterThanOrEqual, 8)
        ]));

        Assert.False(collapse.Collapsed);
        Assert.False(collapse.Recovered);
        Assert.Null(collapse.CollapseTick);
        Assert.Equal("no collapse observed", collapse.RecoveryStatus);
    }

    [Fact]
    public void DefaultCriteriaIncludeExplicitCollapseDefinitions()
    {
        Assert.Contains(FailureAnalysis.DefaultCriteria, criterion =>
            criterion is
            {
                Metric: "Needs Met",
                Operator: FailureOperator.LessThan,
                Threshold: 0.5,
                ThresholdKind: FailureThresholdKind.Absolute
            });
        Assert.Contains(FailureAnalysis.DefaultCriteria, criterion =>
            criterion is
            {
                Metric: "Severe Failures",
                Operator: FailureOperator.GreaterThanOrEqual,
                Threshold: 0.10,
                ThresholdKind: FailureThresholdKind.PopulationShare
            });
    }

    private static SimulationRunResult Result(params MetricResult[] metrics)
    {
        return Result(10, metrics);
    }

    private static SimulationRunResult Result(int population, params MetricResult[] metrics)
    {
        return new SimulationRunResult
        {
            ModelResults =
            [
                new ModelRunResult
                {
                    ModelName = "ModelA",
                    World = new WorldState
                    {
                        Population = new Population
                        {
                            Citizens = Enumerable.Range(0, population).Select(_ => new Citizen()).ToList()
                        }
                    },
                    Metrics = metrics
                }
            ]
        };
    }

    private static MetricResult Metric(string name, int tick, double value)
    {
        return new MetricResult
        {
            Name = name,
            Tick = tick,
            Value = value
        };
    }
}
