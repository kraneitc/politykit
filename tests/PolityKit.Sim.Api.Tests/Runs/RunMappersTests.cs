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

    private static StoredRun CreateStoredRun()
    {
        return new StoredRun
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
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
                                Value = 50,
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
                                Value = 0.9,
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
                                Value = 0.25,
                                Unit = "ratio"
                            }
                        ]
                    }
                ]
            }
        };
    }
}
