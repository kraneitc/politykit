using PolityKit.Sim.Core.World;
using PolityKit.Sim.Metrics;

namespace PolityKit.Sim.Tests.Metrics;

public sealed class NeedsMetMetricTests
{
    [Fact]
    public void CalculateReturnsOneWhenPopulationIsEmpty()
    {
        var metric = new NeedsMetMetric();

        var value = metric.Calculate(new WorldState(), []);

        Assert.Equal(1.0, value);
    }

    [Fact]
    public void CalculateReturnsShareOfCitizensWithAllNeedsMet()
    {
        var world = new WorldState
        {
            Population =
            {
                Citizens =
                {
                    new Citizen(),
                    new Citizen { FoodNeed = 1 },
                    new Citizen { HealthNeed = 1 },
                    new Citizen { HousingNeed = 1 }
                }
            }
        };
        var metric = new NeedsMetMetric();

        var value = metric.Calculate(world, []);

        Assert.Equal(0.25, value);
    }

    [Fact]
    public void CalculateRejectsNullWorld()
    {
        var metric = new NeedsMetMetric();

        Assert.Throws<ArgumentNullException>(() => metric.Calculate(null!, []));
    }
}
