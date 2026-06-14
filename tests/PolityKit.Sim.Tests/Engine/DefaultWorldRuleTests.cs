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
        Assert.Contains(world.Events, simulationEvent =>
            simulationEvent.Type == "ResourceAllocated"
            && Equals(simulationEvent.Data["citizenId"], highPriorityCitizen.Id));
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
        Assert.Contains(world.Events, simulationEvent => simulationEvent.Type == "AppealReview");
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
        Assert.Contains(world.Events, simulationEvent => simulationEvent.Type == "UnmetNeeds");
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
        Assert.Contains(world.Events, simulationEvent => simulationEvent.Type == "AdministrativeBacklog");
    }
}
