using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Tests.Engine;

public sealed class DefaultWorldFactoryTests
{
    [Fact]
    public void CreateWorldCopiesInitialResources()
    {
        var scenario = new ScenarioDefinition
        {
            InitialResources = new ResourcePool
            {
                Food = 10,
                Medicine = 20,
                Housing = 30,
                AdminCapacity = 40,
                ProductionCapacity = 50
            }
        };
        var factory = new DefaultWorldFactory();

        var world = factory.CreateWorld(scenario, new SeededRandomSource(1));

        Assert.NotSame(scenario.InitialResources, world.Resources);
        Assert.Equal(10, world.Resources.Food);
        Assert.Equal(20, world.Resources.Medicine);
        Assert.Equal(30, world.Resources.Housing);
        Assert.Equal(40, world.Resources.AdminCapacity);
        Assert.Equal(50, world.Resources.ProductionCapacity);
    }

    [Fact]
    public void CreateWorldInitializesPopulationFromScenario()
    {
        var scenario = new ScenarioDefinition
        {
            InitialPopulation = 5
        };
        var factory = new DefaultWorldFactory();

        var world = factory.CreateWorld(scenario, new SeededRandomSource(1));

        Assert.Equal(5, world.Population.Count);
        Assert.All(world.Population.Citizens, citizen =>
        {
            Assert.InRange(citizen.FoodNeed, 1, 3);
            Assert.InRange(citizen.HealthNeed, 0, 2);
            Assert.InRange(citizen.HousingNeed, 0, 1);
            Assert.InRange(citizen.TrustInSystem, 45, 85);
        });
    }

    [Fact]
    public void CreateWorldIsDeterministicForSameSeed()
    {
        var scenario = new ScenarioDefinition
        {
            InitialPopulation = 3
        };
        var factory = new DefaultWorldFactory();

        var first = factory.CreateWorld(scenario, new SeededRandomSource(123));
        var second = factory.CreateWorld(scenario, new SeededRandomSource(123));

        Assert.Equal(
            first.Population.Citizens.Select(Snapshot),
            second.Population.Citizens.Select(Snapshot));
    }

    [Fact]
    public void CreateWorldInitializesInstitutionalCapacityAndTrust()
    {
        var scenario = new ScenarioDefinition
        {
            InitialResources = new ResourcePool
            {
                AdminCapacity = 17
            }
        };
        var factory = new DefaultWorldFactory();

        var world = factory.CreateWorld(scenario, new SeededRandomSource(1));

        Assert.Equal(17, world.Institutions.AdministrativeCapacity);
        Assert.Equal(70, world.Institutions.Trust);
    }

    private static object Snapshot(Citizen citizen)
    {
        return new
        {
            citizen.FoodNeed,
            citizen.HealthNeed,
            citizen.HousingNeed,
            citizen.Wealth,
            citizen.SocialPower,
            citizen.TrustInSystem,
            citizen.Vulnerability
        };
    }
}
