using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;
using PolityKit.Sim.Engine;
using System.Text.Json;

namespace PolityKit.Sim.Tests.Engine;

public sealed class SimulationRunSummaryTests
{
    [Fact]
    public void CreateKeepsFinalMetricsAndEventCounts()
    {
        var result = new SimulationRunResult
        {
            ScenarioName = "Scenario A",
            Seed = 123,
            Ticks = 3,
            ModelResults =
            [
                new ModelRunResult
                {
                    ModelName = "Model A",
                    ModelVersion = "1.0",
                    Events =
                    [
                        new SimulationEvent { Tick = 0, Type = "CropFailure" },
                        new SimulationEvent { Tick = 1, Type = "UnmetNeeds" },
                        new SimulationEvent { Tick = 1, Type = "UnmetNeeds" }
                    ],
                    Metrics =
                    [
                        new MetricResult { Name = "Trust", Tick = 0, Value = 80, Unit = "points" },
                        new MetricResult { Name = "Trust", Tick = 2, Value = 70, Unit = "points" },
                        new MetricResult { Name = "Needs Met", Tick = 1, Value = 0.5, Unit = "ratio" },
                        new MetricResult { Name = "Needs Met", Tick = 2, Value = 0.75, Unit = "ratio" }
                    ]
                }
            ]
        };

        var summary = SimulationRunSummary.Create(result);

        Assert.Equal("Scenario A", summary.ScenarioName);
        var model = Assert.Single(summary.Models);
        Assert.Equal(3, model.EventCount);
        Assert.Equal(1, model.EventCountsByType["CropFailure"]);
        Assert.Equal(2, model.EventCountsByType["UnmetNeeds"]);

        var finalMetrics = model.FinalMetrics.Select(metric => (metric.Name, metric.Tick, metric.Value)).ToArray();
        Assert.Equal([("Needs Met", 2, 0.75), ("Trust", 2, 70)], finalMetrics);
    }

    [Fact]
    public void CreateHighlightsLargestNotableMetricChangeWithNearbyEvents()
    {
        var result = new SimulationRunResult
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
                            Tick = 1,
                            Type = "CropFailure",
                            Description = "Food production fell.",
                            Data = { ["severity"] = 0.4 }
                        },
                        new SimulationEvent
                        {
                            Tick = 2,
                            Type = "ResourceAllocated",
                            Description = "Allocated food."
                        },
                        new SimulationEvent
                        {
                            Tick = 3,
                            Type = "UnmetNeeds",
                            Description = "Needs rose.",
                            Data = { ["unmetNeed"] = 100 }
                        }
                    ],
                    Metrics =
                    [
                        new MetricResult { Name = "Needs Met", Tick = 0, Value = 0.9, Unit = "ratio" },
                        new MetricResult { Name = "Needs Met", Tick = 1, Value = 0.88, Unit = "ratio" },
                        new MetricResult { Name = "Needs Met", Tick = 2, Value = 0.6, Unit = "ratio" },
                        new MetricResult { Name = "Trust", Tick = 0, Value = 80, Unit = "points" },
                        new MetricResult { Name = "Trust", Tick = 1, Value = 78, Unit = "points" }
                    ]
                }
            ]
        };

        var model = Assert.Single(SimulationRunSummary.Create(result).Models);

        var change = Assert.Single(model.NotableMetricChanges);
        Assert.Equal("Needs Met", change.Metric);
        Assert.Equal(0, change.FromTick);
        Assert.Equal(2, change.ToTick);
        Assert.Equal(0.9, change.PreviousValue);
        Assert.Equal(0.6, change.Value);
        Assert.Equal("decreased", change.Direction);
        Assert.Equal(0.3, change.AbsoluteChange, precision: 10);
        Assert.Equal("Needs Met dropped after CropFailure at tick 1.", change.Breadcrumb);

        Assert.Equal(["CropFailure", "UnmetNeeds"], change.NearbyEvents.Select(simEvent => simEvent.Type).ToArray());
        Assert.DoesNotContain(change.NearbyEvents, simEvent => simEvent.Type == "ResourceAllocated");
    }

    [Fact]
    public void CreateOmitsChangesBelowMetricThresholds()
    {
        var result = new SimulationRunResult
        {
            ScenarioName = "Scenario A",
            Seed = 123,
            Ticks = 2,
            ModelResults =
            [
                new ModelRunResult
                {
                    ModelName = "Model A",
                    ModelVersion = "1.0",
                    Metrics =
                    [
                        new MetricResult { Name = "Trust", Tick = 0, Value = 80 },
                        new MetricResult { Name = "Trust", Tick = 1, Value = 78 },
                        new MetricResult { Name = "Needs Met", Tick = 0, Value = 0.9 },
                        new MetricResult { Name = "Needs Met", Tick = 1, Value = 0.86 }
                    ]
                }
            ]
        };

        var model = Assert.Single(SimulationRunSummary.Create(result).Models);

        Assert.Empty(model.NotableMetricChanges);
    }

    [Fact]
    public void CreateUsesTickRangeBreadcrumbWhenNoNearbyEventExists()
    {
        var result = new SimulationRunResult
        {
            ScenarioName = "Scenario A",
            Seed = 123,
            Ticks = 10,
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
                            Type = "CorruptionSpike"
                        }
                    ],
                    Metrics =
                    [
                        new MetricResult { Name = "Administrative Load", Tick = 4, Value = 1 },
                        new MetricResult { Name = "Administrative Load", Tick = 7, Value = 6 }
                    ]
                }
            ]
        };

        var model = Assert.Single(SimulationRunSummary.Create(result).Models);
        var change = Assert.Single(model.NotableMetricChanges);

        Assert.Equal("Administrative Load rose between ticks 4 and 7.", change.Breadcrumb);
        Assert.Empty(change.NearbyEvents);
    }

    [Fact]
    public void CreateLinksMetricMovementToNearbyShockEvent()
    {
        var result = new SimulationRunResult
        {
            ScenarioName = "Corruption Scenario",
            Seed = 123,
            Ticks = 35,
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
                            Tick = 30,
                            Type = "CorruptionSpike",
                            Description = "Corruption increased.",
                            Data =
                            {
                                ["severity"] = 0.4,
                                ["institutionalTrustDelta"] = -20
                            }
                        }
                    ],
                    Metrics =
                    [
                        new MetricResult { Name = "Trust", Tick = 29, Value = 75, Unit = "points" },
                        new MetricResult { Name = "Trust", Tick = 31, Value = 50, Unit = "points" }
                    ]
                }
            ]
        };

        var model = Assert.Single(SimulationRunSummary.Create(result).Models);
        var change = Assert.Single(model.NotableMetricChanges);

        Assert.Equal("Trust dropped after CorruptionSpike at tick 30.", change.Breadcrumb);
        var linkedEvent = Assert.Single(change.NearbyEvents);
        Assert.Equal("CorruptionSpike", linkedEvent.Type);
        Assert.Equal(-20, linkedEvent.Data["institutionalTrustDelta"]);
    }

    [Fact]
    public void SummarySerializesInterpretabilityOutput()
    {
        var summary = SimulationRunSummary.Create(new SimulationRunResult
        {
            ScenarioName = "Scenario A",
            Seed = 123,
            Ticks = 3,
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
                            Tick = 1,
                            Type = "AdministrativeOverload",
                            Description = "Administration overloaded.",
                            Data = { ["overflow"] = 5 }
                        }
                    ],
                    Metrics =
                    [
                        new MetricResult { Name = "Administrative Load", Tick = 0, Value = 0 },
                        new MetricResult { Name = "Administrative Load", Tick = 2, Value = 5 }
                    ]
                }
            ]
        });

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(summary));
        var model = document.RootElement.GetProperty("Models")[0];
        Assert.Equal(1, model.GetProperty("EventCountsByType").GetProperty("AdministrativeOverload").GetInt32());

        var change = model.GetProperty("NotableMetricChanges")[0];
        Assert.Equal("Administrative Load", change.GetProperty("Metric").GetString());
        Assert.Equal("Administrative Load rose after AdministrativeOverload at tick 1.", change.GetProperty("Breadcrumb").GetString());
        Assert.Equal("AdministrativeOverload", change.GetProperty("NearbyEvents")[0].GetProperty("Type").GetString());
        Assert.Equal(5, change.GetProperty("NearbyEvents")[0].GetProperty("Data").GetProperty("overflow").GetInt32());
    }
}
