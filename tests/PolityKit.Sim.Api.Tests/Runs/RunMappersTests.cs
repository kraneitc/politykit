using PolityKit.Sim.Api.Services;
using PolityKit.Sim.Api.Services.Models;
using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Api.Tests.Runs;

public sealed class RunMappersTests
{
    [Fact]
    public void ToSummaryResponseMapsRunOverview()
    {
        var run = CreateStoredRun();

        var response = RunMappers.ToSummaryResponse(run);

        Assert.Equal(run.Id, response.Id);
        Assert.Equal(run.CreatedAt, response.CreatedAt);
        Assert.Equal("Scenario A", response.ScenarioName);
        Assert.Equal(123, response.Seed);
        Assert.Equal(5, response.Ticks);
        Assert.Equal(["Model A", "Model B"], response.Models);
    }

    [Fact]
    public void ToDetailResponseMapsModelsAndLatestMetricPerName()
    {
        var run = CreateStoredRun();

        var response = RunMappers.ToDetailResponse(run);

        var firstModel = response.Models.Single(model => model.ModelName == "Model A");
        Assert.Equal("1.0", firstModel.ModelVersion);
        Assert.Equal(2, firstModel.EventCount);
        Assert.Equal(["Needs Met", "Trust"], firstModel.FinalMetrics.Select(metric => metric.Name).ToArray());

        var needsMet = firstModel.FinalMetrics.Single(metric => metric.Name == "Needs Met");
        Assert.Equal("Model A", needsMet.Model);
        Assert.Equal(4, needsMet.Tick);
        Assert.Equal(0.9, needsMet.Value);
        Assert.Equal("ratio", needsMet.Unit);
    }

    [Fact]
    public void ToMetricsFlattensMetricsAcrossModels()
    {
        var run = CreateStoredRun();

        var metrics = RunMappers.ToMetrics(run);

        Assert.Equal(4, metrics.Count);
        Assert.Contains(metrics, metric => metric is { Model: "Model A", Name: "Trust", Tick: 3 });
        Assert.Contains(metrics, metric => metric is { Model: "Model B", Name: "Needs Met", Tick: 4 });
    }

    [Fact]
    public void ToEventsFlattensEventsAcrossModels()
    {
        var run = CreateStoredRun();

        var events = RunMappers.ToEvents(run);

        Assert.Equal(3, events.Count);
        Assert.Contains(events, simulationEvent =>
            simulationEvent is { Model: "Model A", Type: "ResourceAllocated" }
            && Equals(simulationEvent.Data["amount"], 1));
        Assert.Contains(events, simulationEvent =>
            simulationEvent is { Model: "Model B", Type: "UnmetNeeds" });
    }

    [Fact]
    public void ToDashboardResponseIncludesSummaryMetricsAndEvents()
    {
        var run = CreateStoredRun();

        var response = RunMappers.ToDashboardResponse(run);

        Assert.Equal(run.Id, response.Id);
        Assert.Equal("Scenario A", response.ScenarioName);
        Assert.Equal(4, response.Metrics.Count);
        Assert.Equal(3, response.Events.Count);
        Assert.Equal("Scenario A", response.Summary.ScenarioName);
        Assert.Equal(["Model A", "Model B"], response.Summary.Models.Select(model => model.ModelName).ToArray());

        var firstModel = response.Summary.Models.Single(model => model.ModelName == "Model A");
        Assert.Equal(2, firstModel.EventCount);
        Assert.Contains(firstModel.FinalMetrics, metric => metric.Name == "Needs Met" && metric.Value == 0.9);
    }

    [Fact]
    public void ToComparisonResponseMapsFinalMetricDeltas()
    {
        var baseline = CreateStoredRun();
        var comparison = CreateStoredRun(valueOffset: 0.1);

        var response = RunMappers.ToComparisonResponse(baseline, comparison);

        Assert.Equal(baseline.Id, response.Baseline.Id);
        Assert.Equal(comparison.Id, response.Comparison.Id);

        var needsMet = response.MetricDeltas.Single(delta =>
            delta is { Model: "Model A", Metric: "Needs Met" });
        Assert.Equal("ratio", needsMet.Unit);
        Assert.Equal(4, needsMet.BaselineTick);
        Assert.Equal(4, needsMet.ComparisonTick);
        Assert.Equal(0.9, needsMet.BaselineValue!.Value);
        Assert.Equal(1.0, needsMet.ComparisonValue!.Value);
        Assert.Equal(0.1, needsMet.Change!.Value, precision: 10);
        Assert.Equal(0.1 / 0.9, needsMet.PercentChange!.Value, precision: 10);
        Assert.Equal("increased", needsMet.Direction);

        var trust = response.MetricDeltas.Single(delta =>
            delta is { Model: "Model A", Metric: "Trust" });
        Assert.Equal("points", trust.Unit);
        Assert.Equal(50, trust.BaselineValue!.Value);
        Assert.Equal(50.1, trust.ComparisonValue!.Value);
    }

    [Fact]
    public void ToComparisonResponseIncludesMissingMetricSide()
    {
        var baseline = CreateStoredRun();
        var comparison = CreateStoredRunWithoutModelB();

        var response = RunMappers.ToComparisonResponse(baseline, comparison);

        var missing = response.MetricDeltas.Single(delta =>
            delta is { Model: "Model B", Metric: "Needs Met" });
        Assert.Equal(0.25, missing.BaselineValue!.Value);
        Assert.Null(missing.ComparisonValue);
        Assert.Null(missing.Change);
        Assert.Null(missing.PercentChange);
        Assert.Equal("unavailable", missing.Direction);
    }

    private static StoredRun CreateStoredRun(double valueOffset = 0)
    {
        return new StoredRun
        {
            Id = valueOffset == 0
                ? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
                : Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            CreatedAt = DateTimeOffset.Parse("2026-06-14T03:00:00Z"),
            Result = new SimulationRunResult
            {
                ScenarioName = "Scenario A",
                Seed = 123,
                Ticks = 5,
                ModelResults =
                [
                    new ModelRunResult
                    {
                        ModelName = "Model A",
                        ModelVersion = "1.0",
                        Events =
                        [
                            new SimulationEvent
                            {
                                Tick = 0,
                                Type = "ResourceAllocated",
                                Description = "Allocated resource.",
                                Data = { ["amount"] = 1 }
                            },
                            new SimulationEvent
                            {
                                Tick = 1,
                                Type = "PolicyChanged"
                            }
                        ],
                        Metrics =
                        [
                            new MetricResult
                            {
                                Name = "Trust",
                                Tick = 3,
                                Value = 50 + valueOffset,
                                Unit = "points"
                            },
                            new MetricResult
                            {
                                Name = "Needs Met",
                                Tick = 2,
                                Value = 0.5,
                                Unit = "ratio"
                            },
                            new MetricResult
                            {
                                Name = "Needs Met",
                                Tick = 4,
                                Value = 0.9 + valueOffset,
                                Unit = "ratio"
                            }
                        ]
                    },
                    new ModelRunResult
                    {
                        ModelName = "Model B",
                        ModelVersion = "1.0",
                        Events =
                        [
                            new SimulationEvent
                            {
                                Tick = 2,
                                Type = "UnmetNeeds"
                            }
                        ],
                        Metrics =
                        [
                            new MetricResult
                            {
                                Name = "Needs Met",
                                Tick = 4,
                                Value = 0.25 + valueOffset,
                                Unit = "ratio"
                            }
                        ]
                    }
                ]
            }
        };
    }

    private static StoredRun CreateStoredRunWithoutModelB()
    {
        var run = CreateStoredRun();
        return new StoredRun
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            CreatedAt = run.CreatedAt,
            Result = new SimulationRunResult
            {
                ScenarioName = run.Result.ScenarioName,
                Seed = run.Result.Seed,
                Ticks = run.Result.Ticks,
                ModelResults = run.Result.ModelResults
                    .Where(model => model.ModelName != "Model B")
                    .ToArray()
            }
        };
    }
}
