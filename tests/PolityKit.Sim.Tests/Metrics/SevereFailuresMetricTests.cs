using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Metrics;

namespace PolityKit.Sim.Tests.Metrics;

public sealed class SevereFailuresMetricTests
{
    [Fact]
    public void CalculateCountsSevereEventsAndCitizensInSevereNeed()
    {
        var world = new WorldState
        {
            Population =
            {
                Citizens =
                {
                    new Citizen { FoodNeed = 3 },
                    new Citizen { HealthNeed = 2 },
                    new Citizen { TrustInSystem = 10 },
                    new Citizen { FoodNeed = 1, HealthNeed = 1, TrustInSystem = 50 }
                }
            }
        };
        var events = new[]
        {
            new SimulationEvent { Type = "SevereFailure" },
            new SimulationEvent { Type = "AdministrativeBacklog" },
            new SimulationEvent
            {
                Type = "UnmetNeeds",
                Data = { ["unmetNeed"] = 2 }
            },
            new SimulationEvent
            {
                Type = "UnmetNeeds",
                Data = { ["unmetNeed"] = 0 }
            }
        };
        var metric = new SevereFailuresMetric();

        var value = metric.Calculate(world, events);

        Assert.Equal(6, value);
    }

    [Fact]
    public void CalculateTreatsMissingUnmetNeedAsNonSevere()
    {
        var events = new[]
        {
            new SimulationEvent { Type = "UnmetNeeds" }
        };
        var metric = new SevereFailuresMetric();

        var value = metric.Calculate(new WorldState(), events);

        Assert.Equal(0, value);
    }

    [Fact]
    public void CalculateReadsConvertibleUnmetNeedValues()
    {
        var events = new[]
        {
            new SimulationEvent
            {
                Type = "UnmetNeeds",
                Data = { ["unmetNeed"] = "1.5" }
            }
        };
        var metric = new SevereFailuresMetric();

        var value = metric.Calculate(new WorldState(), events);

        Assert.Equal(1, value);
    }

    [Fact]
    public void CalculateRejectsNullInputs()
    {
        var metric = new SevereFailuresMetric();

        Assert.Throws<ArgumentNullException>(() => metric.Calculate(null!, []));
        Assert.Throws<ArgumentNullException>(() => metric.Calculate(new WorldState(), null!));
    }
}
