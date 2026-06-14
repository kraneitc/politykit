using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Models;

namespace PolityKit.Sim.Tests.Models;

public sealed class MarketBasedAllocationTests
{
    [Fact]
    public void DecideAllocatesOnlyForExistingNeeds()
    {
        var citizen = new Citizen
        {
            FoodNeed = 0,
            HealthNeed = 2,
            HousingNeed = 1,
            Wealth = 100
        };
        var world = new WorldState
        {
            Population = { Citizens = { citizen } }
        };
        var model = new MarketBasedAllocation();

        var decision = model.Decide(world, new SystemContext());

        Assert.Equal(3, decision.Allocations.Count);
        Assert.DoesNotContain(decision.Allocations, allocation => allocation.Resource == ResourceKind.Food);
        Assert.Equal(2, decision.Allocations.Count(allocation => allocation.Resource == ResourceKind.Medicine));
        Assert.Single(decision.Allocations, allocation => allocation.Resource == ResourceKind.Housing);
    }

    [Fact]
    public void DecideCalculatesPriorityFromWealthAndDemandWeights()
    {
        var citizen = new Citizen
        {
            FoodNeed = 2,
            HealthNeed = 1,
            HousingNeed = 0,
            Wealth = 50
        };
        var world = new WorldState
        {
            Population = { Citizens = { citizen } }
        };
        var context = new SystemContext
        {
            Parameters = new Dictionary<string, double>
            {
                ["wealthPriorityWeight"] = 1.5,
                ["needDemandWeight"] = 0.5
            }
        };
        var model = new MarketBasedAllocation();

        var decision = model.Decide(world, context);

        Assert.All(decision.Allocations, allocation => Assert.Equal(90, allocation.Priority));
        Assert.All(decision.Allocations, allocation =>
            Assert.Equal("Prioritized by wealth and demand pressure.", allocation.Reason));
    }

    [Fact]
    public void DecideAddsLowAdministrationSettlementAction()
    {
        var world = new WorldState();
        for (var index = 0; index < 120; index++)
        {
            world.Population.Citizens.Add(new Citizen());
        }
        var model = new MarketBasedAllocation();

        var decision = model.Decide(world, new SystemContext());

        var action = Assert.Single(decision.InstitutionalActions);
        Assert.Equal("MarketAllocationSettlement", action.Type);
        Assert.Equal(2, action.AdministrativeCost);
    }

    [Fact]
    public void DecideRejectsNullInputs()
    {
        var model = new MarketBasedAllocation();

        Assert.Throws<ArgumentNullException>(() => model.Decide(null!, new SystemContext()));
        Assert.Throws<ArgumentNullException>(() => model.Decide(new WorldState(), null!));
    }

    [Fact]
    public void ManifestDescribesMarketBasedModel()
    {
        var model = new MarketBasedAllocation();

        Assert.Equal("MarketBasedAllocation", model.Manifest.Model);
        Assert.Equal(model.Version, model.Manifest.Version);
        Assert.Contains(model.Manifest.Assumptions, assumption => assumption.Name == "wealthPriorityWeight");
        Assert.Contains(model.Manifest.KnownFailureModes, mode => mode == "resource concentration");
    }
}
