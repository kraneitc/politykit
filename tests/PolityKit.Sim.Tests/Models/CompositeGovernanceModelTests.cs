using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.Models.Governance;
using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Models;

namespace PolityKit.Sim.Tests.Models;

public sealed class CompositeGovernanceModelTests
{
    [Fact]
    public void DecideUsesAllocationMechanismToChangePriorityDirection()
    {
        var highNeed = new Citizen
        {
            FoodNeed = 3,
            Wealth = 0,
            TrustInSystem = 70
        };
        var highWealth = new Citizen
        {
            FoodNeed = 1,
            Wealth = 200,
            TrustInSystem = 70
        };
        var world = new WorldState
        {
            Population = { Citizens = { highNeed, highWealth } }
        };
        var needModel = new CompositeGovernanceModel(CreateProfile(
            allocationMechanism: Value(GovernanceDimension.AllocationMechanism, "need-weighted", "Need Weighted")));
        var marketModel = new CompositeGovernanceModel(CreateProfile(
            allocationMechanism: Value(GovernanceDimension.AllocationMechanism, "market-weighted", "Market Weighted")));

        var needDecision = needModel.Decide(world, new SystemContext());
        var marketDecision = marketModel.Decide(world, new SystemContext());

        Assert.True(PriorityFor(needDecision, highNeed) > PriorityFor(needDecision, highWealth));
        Assert.True(PriorityFor(marketDecision, highWealth) > PriorityFor(marketDecision, highNeed));
    }

    [Fact]
    public void DecideUsesDecisionAuthorityToChangeAdministrativeCost()
    {
        var participatory = new CompositeGovernanceModel(CreateProfile(
            decisionAuthority: Value(GovernanceDimension.DecisionAuthority, "participatory-assembly", "Participatory Assembly")));
        var centralized = new CompositeGovernanceModel(CreateProfile(
            decisionAuthority: Value(GovernanceDimension.DecisionAuthority, "central-bureau", "Central Bureau")));
        var world = CreateWorld(80);

        var participatoryAction = participatory.Decide(world, new SystemContext())
            .InstitutionalActions.Single(action => action.Type == "CompositeGovernanceDecision");
        var centralizedAction = centralized.Decide(world, new SystemContext())
            .InstitutionalActions.Single(action => action.Type == "CompositeGovernanceDecision");

        Assert.Equal(10, participatoryAction.AdministrativeCost);
        Assert.Equal(3, centralizedAction.AdministrativeCost);
    }

    [Fact]
    public void DecideUsesAccountabilityAndAppealDimensionsForInstitutionalActions()
    {
        var openProfile = CreateProfile(
            accountabilityMechanism: Value(GovernanceDimension.AccountabilityMechanism, "audit", "Audit"),
            appealProcess: Value(GovernanceDimension.AppealProcess, "formal-review", "Formal Review"));
        var closedProfile = CreateProfile(
            accountabilityMechanism: Value(GovernanceDimension.AccountabilityMechanism, "none", "None"),
            appealProcess: Value(GovernanceDimension.AppealProcess, "closed", "Closed"));
        var openModel = new CompositeGovernanceModel(openProfile);
        var closedModel = new CompositeGovernanceModel(closedProfile);

        var openActions = openModel.Decide(CreateWorld(25), new SystemContext()).InstitutionalActions;
        var closedActions = closedModel.Decide(CreateWorld(25), new SystemContext()).InstitutionalActions;

        Assert.Contains(openActions, action => action.Type == "CompositeGovernanceAccountability");
        Assert.Contains(openActions, action => action.Type == "CompositeGovernanceAppeal");
        Assert.DoesNotContain(closedActions, action => action.Type == "CompositeGovernanceAccountability");
        Assert.DoesNotContain(closedActions, action => action.Type == "CompositeGovernanceAppeal");
    }

    [Fact]
    public void DecideUsesInformationFlowAndAppealProcessToRaiseVulnerabilityPriority()
    {
        var lowVulnerability = new Citizen
        {
            FoodNeed = 1,
            Vulnerability = 0
        };
        var highVulnerability = new Citizen
        {
            FoodNeed = 1,
            Vulnerability = 100
        };
        var world = new WorldState
        {
            Population = { Citizens = { lowVulnerability, highVulnerability } }
        };
        var model = new CompositeGovernanceModel(CreateProfile(
            informationFlow: Value(GovernanceDimension.InformationFlow, "transparent", "Transparent"),
            appealProcess: Value(GovernanceDimension.AppealProcess, "formal-review", "Formal Review")));

        var decision = model.Decide(world, new SystemContext());

        Assert.True(PriorityFor(decision, highVulnerability) > PriorityFor(decision, lowVulnerability));
    }

    [Fact]
    public void DecideUsesPropertyRegimeToChangeWealthPriority()
    {
        var highNeed = new Citizen
        {
            FoodNeed = 2,
            Wealth = 0
        };
        var highWealth = new Citizen
        {
            FoodNeed = 1,
            Wealth = 200
        };
        var world = new WorldState
        {
            Population = { Citizens = { highNeed, highWealth } }
        };
        var commonsModel = new CompositeGovernanceModel(CreateProfile(
            allocationMechanism: Value(GovernanceDimension.AllocationMechanism, "mixed", "Mixed"),
            propertyRegime: Value(GovernanceDimension.PropertyRegime, "commons", "Commons")));
        var privateModel = new CompositeGovernanceModel(CreateProfile(
            allocationMechanism: Value(GovernanceDimension.AllocationMechanism, "mixed", "Mixed"),
            propertyRegime: Value(GovernanceDimension.PropertyRegime, "private-property", "Private Property")));

        var commonsDecision = commonsModel.Decide(world, new SystemContext());
        var privateDecision = privateModel.Decide(world, new SystemContext());

        Assert.True(PriorityFor(commonsDecision, highNeed) > PriorityFor(commonsDecision, highWealth));
        Assert.True(PriorityFor(privateDecision, highWealth) > PriorityFor(privateDecision, highNeed));
    }

    [Fact]
    public void DecideUsesContextParameterMultipliers()
    {
        var highNeed = new Citizen
        {
            FoodNeed = 3,
            Wealth = 0
        };
        var highWealth = new Citizen
        {
            FoodNeed = 1,
            Wealth = 200
        };
        var world = new WorldState
        {
            Population = { Citizens = { highNeed, highWealth } }
        };
        var model = new CompositeGovernanceModel(CreateProfile(
            accountabilityMechanism: Value(GovernanceDimension.AccountabilityMechanism, "none", "None"),
            propertyRegime: Value(GovernanceDimension.PropertyRegime, "private-property", "Private Property")));

        var decision = model.Decide(world, new SystemContext
        {
            Parameters = new Dictionary<string, double>
            {
                ["needWeightMultiplier"] = 0.01,
                ["wealthWeightMultiplier"] = 10.0
            }
        });

        Assert.True(PriorityFor(decision, highWealth) > PriorityFor(decision, highNeed));
    }

    [Fact]
    public void DecideIsDeterministicForSameWorldAndContext()
    {
        var model = new CompositeGovernanceModel(CreateProfile());
        var world = CreateWorld(5);
        var context = new SystemContext
        {
            Parameters = new Dictionary<string, double>
            {
                ["needWeightMultiplier"] = 1.25
            }
        };

        var first = model.Decide(world, context);
        var second = model.Decide(world, context);

        Assert.Equal(
            first.Allocations.Select(allocation => (allocation.CitizenId, allocation.Resource, allocation.Amount, allocation.Priority)).ToArray(),
            second.Allocations.Select(allocation => (allocation.CitizenId, allocation.Resource, allocation.Amount, allocation.Priority)).ToArray());
        Assert.Equal(
            first.InstitutionalActions.Select(action => (action.Type, action.AdministrativeCost)).ToArray(),
            second.InstitutionalActions.Select(action => (action.Type, action.AdministrativeCost)).ToArray());
    }

    [Fact]
    public void ManifestDescribesSelectedProfileAndDimensions()
    {
        var profile = CreateProfile();
        var model = new CompositeGovernanceModel(profile);

        Assert.Equal("CompositeGovernance:test-profile", model.Name);
        Assert.Equal("0.1.0", model.Version);
        Assert.Equal(model.Name, model.Manifest.Model);
        Assert.Equal(model.Version, model.Manifest.Version);
        Assert.Contains("Test Profile", model.Manifest.Description);
        Assert.Contains(model.Manifest.Assumptions, assumption => assumption.Name == "allocation-mechanism");
        Assert.Contains(model.Manifest.Assumptions, assumption => assumption.Description.Contains("Need Weighted"));
        Assert.Contains(model.Manifest.KnownFailureModes, mode => mode == "profile labels are simplified bundles of assumptions");
    }

    [Fact]
    public void ConstructorRejectsInvalidProfile()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new CompositeGovernanceModel(new GovernanceProfile()));

        Assert.Contains("Governance profile '' is invalid:", exception.Message);
        Assert.Contains("Allocation Mechanism is required.", exception.Message);
    }

    [Fact]
    public void DecideRejectsNullInputs()
    {
        var model = new CompositeGovernanceModel(CreateProfile());

        Assert.Throws<ArgumentNullException>(() => model.Decide(null!, new SystemContext()));
        Assert.Throws<ArgumentNullException>(() => model.Decide(new WorldState(), null!));
    }

    private static double PriorityFor(SystemDecision decision, Citizen citizen)
    {
        return decision.Allocations.First(allocation => allocation.CitizenId == citizen.Id).Priority;
    }

    private static WorldState CreateWorld(int population)
    {
        var world = new WorldState();
        for (var index = 0; index < population; index++)
        {
            world.Population.Citizens.Add(new Citizen
            {
                FoodNeed = 1,
                HealthNeed = index % 2,
                HousingNeed = 0,
                Wealth = index,
                SocialPower = population - index,
                Vulnerability = index % 5
            });
        }

        return world;
    }

    private static GovernanceProfile CreateProfile(
        GovernanceDimensionValue? allocationMechanism = null,
        GovernanceDimensionValue? decisionAuthority = null,
        GovernanceDimensionValue? accountabilityMechanism = null,
        GovernanceDimensionValue? informationFlow = null,
        GovernanceDimensionValue? propertyRegime = null,
        GovernanceDimensionValue? appealProcess = null)
    {
        return new GovernanceProfile
        {
            Id = "test-profile",
            Name = "Test Profile",
            Description = "A profile for composite governance model tests.",
            AllocationMechanism = allocationMechanism
                ?? Value(GovernanceDimension.AllocationMechanism, "need-weighted", "Need Weighted"),
            DecisionAuthority = decisionAuthority
                ?? Value(GovernanceDimension.DecisionAuthority, "council", "Council"),
            AccountabilityMechanism = accountabilityMechanism
                ?? Value(GovernanceDimension.AccountabilityMechanism, "audit", "Audit"),
            InformationFlow = informationFlow
                ?? Value(GovernanceDimension.InformationFlow, "transparent", "Transparent"),
            PropertyRegime = propertyRegime
                ?? Value(GovernanceDimension.PropertyRegime, "mixed", "Mixed"),
            AppealProcess = appealProcess
                ?? Value(GovernanceDimension.AppealProcess, "formal-review", "Formal Review")
        };
    }

    private static GovernanceDimensionValue Value(GovernanceDimension dimension, string id, string displayName)
    {
        return new GovernanceDimensionValue(dimension, id, displayName);
    }
}
