using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Models;

namespace PolityKit.Sim.Tests.Models;

public sealed class NeedBasedAllocationTests
{
    [Fact]
    public void DecideAllocatesOneUnitForEachUnmetBasicNeed()
    {
        var citizen = new Citizen
        {
            FoodNeed = 2,
            HealthNeed = 1,
            HousingNeed = 1,
            Vulnerability = 10
        };
        var world = new WorldState
        {
            Population = { Citizens = { citizen } }
        };
        var model = new NeedBasedAllocation();

        var decision = model.Decide(world, new SystemContext());

        Assert.Equal(4, decision.Allocations.Count);
        Assert.Equal(2, decision.Allocations.Count(allocation => allocation.Resource == ResourceKind.Food));
        Assert.Single(decision.Allocations, allocation => allocation.Resource == ResourceKind.Medicine);
        Assert.Single(decision.Allocations, allocation => allocation.Resource == ResourceKind.Housing);
        Assert.All(decision.Allocations, allocation =>
        {
            Assert.Equal(citizen.Id, allocation.CitizenId);
            Assert.Equal(1, allocation.Amount);
            Assert.Equal("Prioritized by unmet need and vulnerability.", allocation.Reason);
        });
    }

    [Fact]
    public void DecideCalculatesPriorityFromNeedAndVulnerabilityWeights()
    {
        var citizen = new Citizen
        {
            FoodNeed = 1,
            HealthNeed = 1,
            HousingNeed = 0,
            Vulnerability = 20
        };
        var world = new WorldState
        {
            Population = { Citizens = { citizen } }
        };
        var context = new SystemContext
        {
            Parameters = new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 2.0,
                ["vulnerabilityPriorityWeight"] = 0.25
            }
        };
        var model = new NeedBasedAllocation();

        var decision = model.Decide(world, context);

        Assert.All(decision.Allocations, allocation => Assert.Equal(405, allocation.Priority));
    }

    [Fact]
    public void DecideAddsNeedAssessmentAdministrativeAction()
    {
        var world = new WorldState();
        for (var index = 0; index < 25; index++)
        {
            world.Population.Citizens.Add(new Citizen());
        }
        var model = new NeedBasedAllocation();

        var decision = model.Decide(world, new SystemContext());

        var action = Assert.Single(decision.InstitutionalActions);
        Assert.Equal("NeedAssessment", action.Type);
        Assert.Equal(2, action.AdministrativeCost);
    }

    [Fact]
    public void DecideRejectsNullInputs()
    {
        var model = new NeedBasedAllocation();

        Assert.Throws<ArgumentNullException>(() => model.Decide(null!, new SystemContext()));
        Assert.Throws<ArgumentNullException>(() => model.Decide(new WorldState(), null!));
    }

    [Fact]
    public void ManifestDescribesNeedBasedModel()
    {
        var model = new NeedBasedAllocation();

        Assert.Equal("NeedBasedAllocation", model.Manifest.Model);
        Assert.Equal(model.Version, model.Manifest.Version);
        Assert.Contains(model.Manifest.Assumptions, assumption => assumption.Name == "needPriorityWeight");
        Assert.Contains(model.Manifest.KnownFailureModes, mode => mode == "high assessment overhead");
    }
}
