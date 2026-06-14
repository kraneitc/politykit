using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Scenarios;

public static class BuiltInScenarios
{
    public static ScenarioDefinition VillageFoodCrisis()
    {
        return new ScenarioDefinition
        {
            Name = "Village Food Crisis",
            Seed = 12345,
            Ticks = 120,
            InitialPopulation = 500,
            InitialResources = new ResourcePool
            {
                Food = 800,
                Medicine = 120,
                Housing = 450,
                AdminCapacity = 80,
                ProductionCapacity = 100
            },
            Shocks =
            [
                new ShockDefinition
                {
                    Tick = 20,
                    Type = "CropFailure",
                    Severity = 0.4
                },
                new ShockDefinition
                {
                    Tick = 45,
                    Type = "AdministrativeOverload",
                    Severity = 0.3
                }
            ]
        };
    }
}
