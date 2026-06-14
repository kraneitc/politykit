using PolityKit.Sim.Core.World;
using PolityKit.Sim.Metrics;

namespace PolityKit.Sim.Tests.Metrics;

public sealed class InequalityMetricTests
{
    [Fact]
    public void CalculateReturnsZeroWhenPopulationIsEmpty()
    {
        var metric = new InequalityMetric();

        var value = metric.Calculate(new WorldState(), []);

        Assert.Equal(0.0, value);
    }

    [Fact]
    public void CalculateReturnsZeroWhenTotalWealthIsZero()
    {
        var world = new WorldState
        {
            Population =
            {
                Citizens =
                {
                    new Citizen { Wealth = 0 },
                    new Citizen { Wealth = -5 }
                }
            }
        };
        var metric = new InequalityMetric();

        var value = metric.Calculate(world, []);

        Assert.Equal(0.0, value);
    }

    [Fact]
    public void CalculateClampsNegativeWealthAndComputesGiniCoefficient()
    {
        var world = new WorldState
        {
            Population =
            {
                Citizens =
                {
                    new Citizen { Wealth = -10 },
                    new Citizen { Wealth = 0 },
                    new Citizen { Wealth = 100 }
                }
            }
        };
        var metric = new InequalityMetric();

        var value = metric.Calculate(world, []);

        Assert.Equal(2.0 / 3.0, value, precision: 10);
    }

    [Fact]
    public void CalculateRejectsNullWorld()
    {
        var metric = new InequalityMetric();

        Assert.Throws<ArgumentNullException>(() => metric.Calculate(null!, []));
    }
}
