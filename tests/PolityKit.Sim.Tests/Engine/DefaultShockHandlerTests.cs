using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Tests.Engine;

public sealed class DefaultShockHandlerTests
{
    [Fact]
    public void CanHandleRequiresShockType()
    {
        var handler = new DefaultShockHandler();

        Assert.False(handler.CanHandle(new ShockDefinition()));
        Assert.True(handler.CanHandle(new ShockDefinition { Type = "CropFailure" }));
    }

    [Fact]
    public void ApplyCropFailureReducesFoodAndProductionMultiplier()
    {
        var world = new WorldState
        {
            Resources = { Food = 100 }
        };
        var handler = new DefaultShockHandler();

        handler.Apply(world, new ShockDefinition
        {
            Type = "CropFailure",
            Severity = 0.25
        });

        Assert.Equal(75, world.Resources.Food);
        Assert.Equal(0.75, world.Environment.FoodProductionMultiplier);
        var simEvent = Assert.Single(world.Events);
        Assert.Equal("CropFailure", simEvent.Type);
        Assert.Equal("Food", simEvent.Data["affectedResource"]);
        Assert.Equal(100, simEvent.Data["resourceBefore"]);
        Assert.Equal(75, simEvent.Data["resourceAfter"]);
        Assert.Equal(-25, simEvent.Data["resourceDelta"]);
        Assert.Equal(25, simEvent.Data["resourceReduction"]);
        Assert.Equal(1.0, simEvent.Data["foodProductionMultiplierBefore"]);
        Assert.Equal(0.75, simEvent.Data["foodProductionMultiplierAfter"]);
    }

    [Fact]
    public void ApplyAdministrativeOverloadConsumesCapacityAndAddsLoad()
    {
        var world = new WorldState
        {
            Resources = { AdminCapacity = 20 },
            Institutions = { AdministrativeCapacity = 10 }
        };
        var handler = new DefaultShockHandler();

        handler.Apply(world, new ShockDefinition
        {
            Type = "AdministrativeOverload",
            Severity = 0.5
        });

        Assert.Equal(10, world.Resources.AdminCapacity);
        Assert.Equal(5, world.Institutions.AdministrativeLoad);

        var simEvent = Assert.Single(world.Events);
        Assert.Equal("AdminCapacity", simEvent.Data["affectedResource"]);
        Assert.Equal(20, simEvent.Data["adminCapacityBefore"]);
        Assert.Equal(10, simEvent.Data["adminCapacityAfter"]);
        Assert.Equal(-10, simEvent.Data["adminCapacityDelta"]);
        Assert.Equal(0, simEvent.Data["administrativeLoadBefore"]);
        Assert.Equal(5, simEvent.Data["administrativeLoadAfter"]);
        Assert.Equal(5, simEvent.Data["administrativeLoadDelta"]);
    }

    [Fact]
    public void ApplyCorruptionSpikeClampsCorruptionAndReducesTrust()
    {
        var world = new WorldState
        {
            Institutions =
            {
                Corruption = 0.8,
                Trust = 40
            }
        };
        var handler = new DefaultShockHandler();

        handler.Apply(world, new ShockDefinition
        {
            Type = "CorruptionSpike",
            Severity = 0.5
        });

        Assert.Equal(1.0, world.Institutions.Corruption);
        Assert.Equal(0, world.Institutions.Trust);

        var simEvent = Assert.Single(world.Events);
        Assert.Equal(0.8, simEvent.Data["corruptionBefore"]);
        Assert.Equal(1.0, simEvent.Data["corruptionAfter"]);
        Assert.Equal(0.2, (double)simEvent.Data["corruptionDelta"], precision: 10);
        Assert.Equal(40, simEvent.Data["institutionalTrustBefore"]);
        Assert.Equal(0, simEvent.Data["institutionalTrustAfter"]);
        Assert.Equal(-40, simEvent.Data["institutionalTrustDelta"]);
    }

    [Fact]
    public void ApplyUnknownShockReducesStabilitySlightly()
    {
        var world = new WorldState();
        var handler = new DefaultShockHandler();

        handler.Apply(world, new ShockDefinition
        {
            Type = "UnknownShock",
            Severity = 0.5
        });

        Assert.Equal(0.95, world.Environment.Stability, precision: 10);
        Assert.Contains(world.Events, simulationEvent => simulationEvent.Type == "UnknownShock");
    }

    [Fact]
    public void ApplyClampsSeverityBeforeApplyingEffects()
    {
        var world = new WorldState
        {
            Resources = { Medicine = 10 }
        };
        var handler = new DefaultShockHandler();

        handler.Apply(world, new ShockDefinition
        {
            Type = "MedicineShortage",
            Severity = 2.0
        });

        Assert.Equal(0, world.Resources.Medicine);
        Assert.Equal(0.0, world.Environment.MedicineSupplyMultiplier);
        Assert.Equal(1.0, world.Events.Single().Data["severity"]);
    }
}
