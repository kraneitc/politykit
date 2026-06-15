using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Tests.Engine;

public sealed class DefaultWorldRuleTests
{
    [Fact]
    public void ApplyAllocatesResourcesByPriority()
    {
        var lowPriorityCitizen = new Citizen { FoodNeed = 5, TrustInSystem = 80 };
        var highPriorityCitizen = new Citizen { FoodNeed = 5, TrustInSystem = 80 };
        var world = new WorldState
        {
            Resources = { Food = 5 },
            Institutions = { Trust = 70 },
            Population =
            {
                Citizens =
                {
                    lowPriorityCitizen,
                    highPriorityCitizen
                }
            }
        };
        var decision = new SystemDecision
        {
            Allocations =
            {
                new ResourceAllocation
                {
                    CitizenId = lowPriorityCitizen.Id,
                    Resource = ResourceKind.Food,
                    Amount = 5,
                    Priority = 1,
                    Reason = "low"
                },
                new ResourceAllocation
                {
                    CitizenId = highPriorityCitizen.Id,
                    Resource = ResourceKind.Food,
                    Amount = 5,
                    Priority = 10,
                    Reason = "high"
                }
            }
        };
        var rule = new DefaultWorldRule();

        rule.Apply(world, decision);

        Assert.Equal(5, lowPriorityCitizen.FoodNeed);
        Assert.Equal(0, highPriorityCitizen.FoodNeed);
        Assert.Equal(0, world.Resources.Food);
        var allocationEvent = world.Events.Single(simulationEvent => simulationEvent.Type == "ResourceAllocated");
        Assert.Equal(highPriorityCitizen.Id, allocationEvent.Data["citizenId"]);
        Assert.Equal("Food", allocationEvent.Data["affectedResource"]);
        Assert.Equal(5, allocationEvent.Data["amount"]);
        Assert.Equal(10.0, allocationEvent.Data["priority"]);
        Assert.Equal(5, allocationEvent.Data["resourceBefore"]);
        Assert.Equal(0, allocationEvent.Data["resourceAfter"]);
        Assert.Equal(-5, allocationEvent.Data["resourceDelta"]);
        Assert.Equal(5, allocationEvent.Data["needBefore"]);
        Assert.Equal(0, allocationEvent.Data["needAfter"]);
        Assert.Equal(-5, allocationEvent.Data["needDelta"]);
        Assert.Equal(5, allocationEvent.Data["citizenTotalNeedBefore"]);
        Assert.Equal(0, allocationEvent.Data["citizenTotalNeedAfter"]);
    }

    [Fact]
    public void ApplyIgnoresInvalidAllocations()
    {
        var citizen = new Citizen { FoodNeed = 5, HealthNeed = 0, HousingNeed = 0 };
        var world = new WorldState
        {
            Resources = { Food = 10 },
            Population = { Citizens = { citizen } }
        };
        var decision = new SystemDecision
        {
            Allocations =
            {
                new ResourceAllocation
                {
                    CitizenId = null,
                    Resource = ResourceKind.Food,
                    Amount = 5,
                    Priority = 10
                },
                new ResourceAllocation
                {
                    CitizenId = citizen.Id,
                    Resource = ResourceKind.Food,
                    Amount = 0,
                    Priority = 10
                }
            }
        };
        var rule = new DefaultWorldRule();

        rule.Apply(world, decision);

        Assert.Equal(5, citizen.FoodNeed);
        Assert.Equal(10, world.Resources.Food);
        Assert.DoesNotContain(world.Events, simulationEvent => simulationEvent.Type == "ResourceAllocated");
    }

    [Fact]
    public void ApplyClampsAllocationAmountToRemainingNeed()
    {
        var citizen = new Citizen { FoodNeed = 2, HealthNeed = 0, HousingNeed = 0 };
        var world = new WorldState
        {
            Resources = { Food = 10 },
            Population = { Citizens = { citizen } }
        };
        var decision = new SystemDecision
        {
            Allocations =
            {
                new ResourceAllocation
                {
                    CitizenId = citizen.Id,
                    Resource = ResourceKind.Food,
                    Amount = 5,
                    Priority = 10
                }
            }
        };
        var rule = new DefaultWorldRule();

        rule.Apply(world, decision);

        Assert.Equal(0, citizen.FoodNeed);
        Assert.Equal(8, world.Resources.Food);
        var allocationEvent = world.Events.Single(simulationEvent => simulationEvent.Type == "ResourceAllocated");
        Assert.Equal(2, allocationEvent.Data["amount"]);
        Assert.Equal(10, allocationEvent.Data["resourceBefore"]);
        Assert.Equal(8, allocationEvent.Data["resourceAfter"]);
        Assert.Equal(-2, allocationEvent.Data["resourceDelta"]);
        Assert.Equal(2, allocationEvent.Data["needBefore"]);
        Assert.Equal(0, allocationEvent.Data["needAfter"]);
    }

    [Fact]
    public void ApplyRecordsPolicyChangesAndInstitutionalActions()
    {
        var world = new WorldState
        {
            Institutions =
            {
                AdministrativeCapacity = 10,
                Trust = 70
            }
        };
        var decision = new SystemDecision
        {
            PolicyChanges =
            {
                new PolicyChange
                {
                    Name = "rationing",
                    Value = 1,
                    Reason = "scarcity"
                }
            },
            InstitutionalActions =
            {
                new InstitutionalAction
                {
                    Type = "AppealReview",
                    Description = "Reviewed appeals",
                    AdministrativeCost = 3
                }
            }
        };
        var rule = new DefaultWorldRule();

        rule.Apply(world, decision);

        Assert.Contains(world.Events, simulationEvent => simulationEvent.Type == "PolicyChanged");
        var actionEvent = world.Events.Single(simulationEvent => simulationEvent.Type == "AppealReview");
        Assert.Equal("AppealReview", actionEvent.Data["actionType"]);
        Assert.Equal(3, actionEvent.Data["administrativeCost"]);
        Assert.Equal(0, actionEvent.Data["administrativeLoadBefore"]);
        Assert.Equal(3, actionEvent.Data["administrativeLoadAfter"]);
        Assert.Equal(3, actionEvent.Data["administrativeLoadDelta"]);
        Assert.Equal(0, world.Institutions.AdministrativeLoad);
    }

    [Fact]
    public void ApplyPenalizesTrustForUnmetNeeds()
    {
        var citizen = new Citizen
        {
            FoodNeed = 4,
            HealthNeed = 2,
            HousingNeed = 0,
            TrustInSystem = 80
        };
        var world = new WorldState
        {
            Institutions = { Trust = 70 },
            Population = { Citizens = { citizen } }
        };
        var rule = new DefaultWorldRule();

        rule.Apply(world, new SystemDecision());

        Assert.Equal(64, world.Institutions.Trust);
        Assert.Equal(74, citizen.TrustInSystem);
        var unmetNeedsEvent = world.Events.Single(simulationEvent => simulationEvent.Type == "UnmetNeeds");
        Assert.Equal(6, unmetNeedsEvent.Data["unmetNeed"]);
        Assert.Equal(1, unmetNeedsEvent.Data["affectedCitizenCount"]);
        Assert.Equal(1, unmetNeedsEvent.Data["populationCount"]);
        Assert.Equal(6.0, unmetNeedsEvent.Data["averageUnmetNeed"]);
        Assert.Equal(4, unmetNeedsEvent.Data["foodNeed"]);
        Assert.Equal(2, unmetNeedsEvent.Data["healthNeed"]);
        Assert.Equal(0, unmetNeedsEvent.Data["housingNeed"]);
        Assert.Equal(70, unmetNeedsEvent.Data["institutionalTrustBefore"]);
        Assert.Equal(64, unmetNeedsEvent.Data["institutionalTrustAfter"]);
        Assert.Equal(-6, unmetNeedsEvent.Data["institutionalTrustDelta"]);
        Assert.Equal(-6, unmetNeedsEvent.Data["citizenTrustDelta"]);
    }

    [Fact]
    public void ApplyCreatesAdministrativeBacklogWhenLoadExceedsCapacity()
    {
        var world = new WorldState
        {
            Institutions =
            {
                AdministrativeCapacity = 3,
                Trust = 70
            },
            Resources = { AdminCapacity = 3 }
        };
        var decision = new SystemDecision
        {
            InstitutionalActions =
            {
                new InstitutionalAction
                {
                    Type = "ManualReview",
                    AdministrativeCost = 8
                }
            }
        };
        var rule = new DefaultWorldRule();

        rule.Apply(world, decision);

        Assert.Equal(5, world.Institutions.AppealBacklog);
        Assert.Equal(65, world.Institutions.Trust);
        Assert.Equal(0, world.Institutions.AdministrativeLoad);
        var backlogEvent = world.Events.Single(simulationEvent => simulationEvent.Type == "AdministrativeBacklog");
        Assert.Equal(3, backlogEvent.Data["capacity"]);
        Assert.Equal(8, backlogEvent.Data["load"]);
        Assert.Equal(5, backlogEvent.Data["overflow"]);
        Assert.Equal(0, backlogEvent.Data["backlogBefore"]);
        Assert.Equal(5, backlogEvent.Data["backlogAfter"]);
        Assert.Equal(5, backlogEvent.Data["backlogDelta"]);
        Assert.Equal(70, backlogEvent.Data["institutionalTrustBefore"]);
        Assert.Equal(65, backlogEvent.Data["institutionalTrustAfter"]);
        Assert.Equal(-5, backlogEvent.Data["institutionalTrustDelta"]);
    }
}
