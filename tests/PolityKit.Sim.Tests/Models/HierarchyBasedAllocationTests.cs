using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Models;

namespace PolityKit.Sim.Tests.Models;

public sealed class HierarchyBasedAllocationTests
{
    [Fact]
    public void DecideAllocatesBasicNeedsForEveryCitizen()
    {
        var first = new Citizen
        {
            FoodNeed = 1,
            HealthNeed = 0,
            HousingNeed = 1,
            SocialPower = 80
        };
        var second = new Citizen
        {
            FoodNeed = 0,
            HealthNeed = 1,
            HousingNeed = 0,
            SocialPower = 10
        };
        var world = new WorldState
        {
            Population = { Citizens = { first, second } }
        };
        var model = new HierarchyBasedAllocation();

        var decision = model.Decide(world, new SystemContext());

        Assert.Equal(3, decision.Allocations.Count);
        Assert.Equal(2, decision.Allocations.Count(allocation => allocation.CitizenId == first.Id));
        Assert.Single(decision.Allocations, allocation => allocation.CitizenId == second.Id);
    }

    [Fact]
    public void DecideCalculatesPriorityFromRankNeedAndVulnerabilityWeights()
    {
        var citizen = new Citizen
        {
            FoodNeed = 1,
            HealthNeed = 1,
            HousingNeed = 0,
            SocialPower = 40,
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
                ["rankPriorityWeight"] = 1.0,
                ["obligationNeedWeight"] = 0.5
            }
        };
        var model = new HierarchyBasedAllocation();

        var decision = model.Decide(world, context);

        Assert.All(decision.Allocations, allocation => Assert.Equal(75, allocation.Priority));
        Assert.All(decision.Allocations, allocation =>
            Assert.Equal("Prioritized by social power, obligation, and visible need.", allocation.Reason));
    }

    [Fact]
    public void DecideAddsHierarchyReviewAdministrativeAction()
    {
        var world = new WorldState();
        for (var index = 0; index < 45; index++)
        {
            world.Population.Citizens.Add(new Citizen());
        }
        var model = new HierarchyBasedAllocation();

        var decision = model.Decide(world, new SystemContext());

        var action = Assert.Single(decision.InstitutionalActions);
        Assert.Equal("HierarchyAllocationReview", action.Type);
        Assert.Equal(2, action.AdministrativeCost);
    }

    [Fact]
    public void DecideRejectsNullInputs()
    {
        var model = new HierarchyBasedAllocation();

        Assert.Throws<ArgumentNullException>(() => model.Decide(null!, new SystemContext()));
        Assert.Throws<ArgumentNullException>(() => model.Decide(new WorldState(), null!));
    }

    [Fact]
    public void ManifestDescribesHierarchyBasedModel()
    {
        var model = new HierarchyBasedAllocation();

        Assert.Equal("HierarchyBasedAllocation", model.Manifest.Model);
        Assert.Equal(model.Version, model.Manifest.Version);
        Assert.Contains(model.Manifest.Assumptions, assumption => assumption.Name == "rankPriorityWeight");
        Assert.Contains(model.Manifest.KnownFailureModes, mode => mode == "elite capture");
    }
}
