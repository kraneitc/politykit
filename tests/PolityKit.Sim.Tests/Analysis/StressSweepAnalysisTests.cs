using PolityKit.Sim.Analysis;

namespace PolityKit.Sim.Tests.Analysis;

public sealed class StressSweepAnalysisTests
{
    [Fact]
    public void BuildPlanExpandsScenariosSeedsModelsAndParameters()
    {
        var plan = StressSweepAnalysis.BuildPlan(new StressSweepRequest
        {
            GridName = "food-ladder",
            Scenarios = ["village-food-crisis", "examples/medicine-shortage.json"],
            Seeds = [111, 222],
            Models = ["need-based-allocation", "market-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["fixedWeight"] = 10
            },
            Sweep = new Dictionary<string, IReadOnlyList<double>>
            {
                ["needPriorityWeight"] = [0.75, 1.0]
            }
        });

        Assert.Equal("food-ladder", plan.GridName);
        Assert.Equal(16, plan.Runs.Count);
        Assert.Equal(Enumerable.Range(1, 16), plan.Runs.Select(run => run.RunIndex));
        Assert.All(plan.Runs, run => Assert.Equal(10, run.Parameters["fixedWeight"]));
        Assert.Contains(plan.Runs, run =>
            run.Scenario == "village-food-crisis"
            && run.Seed == 111
            && run.Model == "need-based-allocation"
            && run.Parameters["needPriorityWeight"] == 0.75);
        Assert.Contains(plan.Runs, run =>
            run.Scenario == "examples/medicine-shortage.json"
            && run.Seed == 222
            && run.Model == "market-based-allocation"
            && run.Parameters["needPriorityWeight"] == 1.0);
    }

    [Fact]
    public void BuildPlanAllowsStressWithoutParameterSweep()
    {
        var plan = StressSweepAnalysis.BuildPlan(new StressSweepRequest
        {
            Scenarios = ["village-food-crisis"],
            Seeds = [111, 222],
            Models = ["need-based-allocation"],
            Parameters = new Dictionary<string, double>
            {
                ["fixedWeight"] = 10
            }
        });

        Assert.Equal(2, plan.Runs.Count);
        Assert.All(plan.Runs, run => Assert.Equal(10, run.Parameters["fixedWeight"]));
        Assert.Empty(plan.Sweep);
    }

    [Fact]
    public void BuildPlanRejectsRunCountsOverLimit()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            StressSweepAnalysis.BuildPlan(new StressSweepRequest
            {
                Scenarios = ["a", "b"],
                Seeds = [1, 2],
                Models = ["m1", "m2"],
                Sweep = new Dictionary<string, IReadOnlyList<double>>
                {
                    ["weight"] = [0.1, 0.2]
                },
                MaxRuns = 15
            }));

        Assert.Equal("Stress sweep would create 16 runs; the maximum is 15.", exception.Message);
    }

    [Fact]
    public void BuildPlanRejectsOverflowSizedSweepBeforeMaterializingRuns()
    {
        var values = Enumerable.Range(0, 50_000).Select(value => (double)value).ToArray();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            StressSweepAnalysis.BuildPlan(new StressSweepRequest
            {
                Scenarios = ["village-food-crisis"],
                Seeds = [1],
                Models = ["need-based-allocation"],
                Sweep = new Dictionary<string, IReadOnlyList<double>>
                {
                    ["a"] = values,
                    ["b"] = values
                },
                MaxRuns = int.MaxValue
            }));

        Assert.Equal("Sweep would create 2500000000 runs; the maximum is 2147483647.", exception.Message);
    }
}
