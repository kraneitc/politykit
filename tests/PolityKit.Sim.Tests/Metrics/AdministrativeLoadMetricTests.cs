using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Metrics;

namespace PolityKit.Sim.Tests.Metrics;

public sealed class AdministrativeLoadMetricTests
{
    [Fact]
    public void CalculateReturnsAppealBacklogWhenThereAreNoBacklogEvents()
    {
        var world = new WorldState
        {
            Institutions = { AppealBacklog = 7 }
        };
        var metric = new AdministrativeLoadMetric();

        var value = metric.Calculate(world, []);

        Assert.Equal(7, value);
    }

    [Fact]
    public void CalculateAddsConvertibleOverflowFromAdministrativeBacklogEvents()
    {
        var world = new WorldState
        {
            Institutions = { AppealBacklog = 7 }
        };
        var events = new[]
        {
            new SimulationEvent
            {
                Type = "AdministrativeBacklog",
                Data = { ["overflow"] = 2 }
            },
            new SimulationEvent
            {
                Type = "AdministrativeBacklog",
                Data = { ["overflow"] = "3.5" }
            },
            new SimulationEvent
            {
                Type = "Other",
                Data = { ["overflow"] = 100 }
            },
            new SimulationEvent { Type = "AdministrativeBacklog" }
        };
        var metric = new AdministrativeLoadMetric();

        var value = metric.Calculate(world, events);

        Assert.Equal(12.5, value);
    }

    [Fact]
    public void CalculateRejectsNullInputs()
    {
        var metric = new AdministrativeLoadMetric();

        Assert.Throws<ArgumentNullException>(() => metric.Calculate(null!, []));
        Assert.Throws<ArgumentNullException>(() => metric.Calculate(new WorldState(), null!));
    }
}
