using PolityKit.Sim.Core.World;
using PolityKit.Sim.Metrics;

namespace PolityKit.Sim.Tests.Metrics;

public sealed class TrustMetricTests
{
    [Fact]
    public void CalculateReturnsInstitutionalTrustWhenPopulationIsEmpty()
    {
        var world = new WorldState
        {
            Institutions = { Trust = 72 }
        };
        var metric = new TrustMetric();

        var value = metric.Calculate(world, []);

        Assert.Equal(72, value);
    }

    [Fact]
    public void CalculateAveragesCitizenTrustWithInstitutionalTrust()
    {
        var world = new WorldState
        {
            Institutions = { Trust = 80 },
            Population =
            {
                Citizens =
                {
                    new Citizen { TrustInSystem = 40 },
                    new Citizen { TrustInSystem = 60 }
                }
            }
        };
        var metric = new TrustMetric();

        var value = metric.Calculate(world, []);

        Assert.Equal(65, value);
    }

    [Fact]
    public void CalculateRejectsNullWorld()
    {
        var metric = new TrustMetric();

        Assert.Throws<ArgumentNullException>(() => metric.Calculate(null!, []));
    }
}
