using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Tests.Core;

public sealed class WorldStateTests
{
    [Fact]
    public void NewWorldStateCreatesUsableNestedState()
    {
        var world = new WorldState();

        Assert.NotNull(world.Population);
        Assert.NotNull(world.Resources);
        Assert.NotNull(world.Institutions);
        Assert.NotNull(world.Environment);
        Assert.NotNull(world.Events);
    }

    [Fact]
    public void PopulationCountReflectsCitizenList()
    {
        var population = new Population
        {
            Citizens =
            {
                new Citizen(),
                new Citizen(),
                new Citizen()
            }
        };

        Assert.Equal(3, population.Count);
    }

    [Fact]
    public void CitizenIdsDefaultToUniqueValues()
    {
        var first = new Citizen();
        var second = new Citizen();

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(Guid.Empty, second.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void EnvironmentDefaultsToNeutralMultipliers()
    {
        var environment = new EnvironmentState();

        Assert.Equal(1.0, environment.FoodProductionMultiplier);
        Assert.Equal(1.0, environment.MedicineSupplyMultiplier);
        Assert.Equal(1.0, environment.HousingAvailabilityMultiplier);
        Assert.Equal(1.0, environment.Stability);
    }

    [Fact]
    public void InstitutionsDefaultToFullLegitimacy()
    {
        var institutions = new InstitutionalState();

        Assert.Equal(1.0, institutions.Legitimacy);
    }

    [Fact]
    public void EventDataStartsUsable()
    {
        var simulationEvent = new SimulationEvent();

        simulationEvent.Data["resource"] = ResourceKind.Food;

        Assert.Equal(ResourceKind.Food, simulationEvent.Data["resource"]);
    }
}
