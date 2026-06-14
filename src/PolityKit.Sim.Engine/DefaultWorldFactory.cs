using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Engine;

public sealed class DefaultWorldFactory : IWorldFactory
{
    public WorldState CreateWorld(ScenarioDefinition scenario, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(random);

        var world = new WorldState
        {
            Resources = Clone(scenario.InitialResources),
            Institutions =
            {
                AdministrativeCapacity = scenario.InitialResources.AdminCapacity,
                Trust = 70
            }
        };

        for (var i = 0; i < scenario.InitialPopulation; i++)
        {
            world.Population.Citizens.Add(new Citizen
            {
                FoodNeed = random.Next(1, 4),
                HealthNeed = random.Next(0, 3),
                HousingNeed = random.Next(0, 2),
                Wealth = random.Next(0, 100),
                SocialPower = random.Next(0, 100),
                TrustInSystem = random.Next(45, 86),
                Vulnerability = random.Next(0, 100)
            });
        }

        return world;
    }

    private static ResourcePool Clone(ResourcePool resources)
    {
        return new ResourcePool
        {
            Food = resources.Food,
            Medicine = resources.Medicine,
            Housing = resources.Housing,
            AdminCapacity = resources.AdminCapacity,
            ProductionCapacity = resources.ProductionCapacity
        };
    }
}
