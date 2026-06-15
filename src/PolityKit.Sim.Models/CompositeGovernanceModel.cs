using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.Models.Governance;
using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Models;

public sealed class CompositeGovernanceModel : AllocationModelBase
{
    private readonly GovernanceProfile _profile;

    public CompositeGovernanceModel(GovernanceProfile profile)
        : this(profile, [], [])
    {
    }

    public CompositeGovernanceModel(GovernancePreset preset)
        : this(GetProfile(preset), preset.Assumptions, preset.KnownFailureModes)
    {
    }

    private CompositeGovernanceModel(
        GovernanceProfile profile,
        IReadOnlyList<string> presetAssumptions,
        IReadOnlyList<string> presetKnownFailureModes)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var validation = GovernanceProfileValidator.Validate(profile);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Governance profile '{profile.Name}' is invalid: {string.Join("; ", validation.Errors)}");
        }

        _profile = profile;
        Manifest = BuildManifest(profile, presetAssumptions, presetKnownFailureModes);
    }

    public GovernanceProfile Profile => _profile;

    public override string Name => $"CompositeGovernance:{_profile.Id}";

    public override string Version => "0.1.0";

    public override ModelManifest Manifest { get; }

    public override SystemDecision Decide(WorldState world, SystemContext context)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);

        var weights = BuildPriorityWeights(context);
        var decision = new SystemDecision();

        foreach (var citizen in world.Population.Citizens)
        {
            var priority = CalculatePriority(citizen, weights);
            decision.Allocations.AddRange(AllocateBasicNeeds(
                citizen,
                priority,
                $"Prioritized by composite governance profile '{_profile.Name}'."));
        }

        decision.InstitutionalActions.AddRange(BuildInstitutionalActions(world));

        return decision;
    }

    private PriorityWeights BuildPriorityWeights(SystemContext context)
    {
        var weights = AllocationMechanismContribution();
        weights = weights
            .Add(DecisionAuthorityContribution())
            .Add(AccountabilityContribution())
            .Add(InformationFlowContribution())
            .Add(PropertyRegimeContribution())
            .Add(AppealProcessContribution());

        return new PriorityWeights(
            weights.NeedWeight * context.Parameters.GetValueOrDefault("needWeightMultiplier", 1.0),
            weights.WealthWeight * context.Parameters.GetValueOrDefault("wealthWeightMultiplier", 1.0),
            weights.RankWeight * context.Parameters.GetValueOrDefault("rankWeightMultiplier", 1.0),
            weights.VulnerabilityWeight * context.Parameters.GetValueOrDefault("vulnerabilityWeightMultiplier", 1.0));
    }

    private PriorityWeights AllocationMechanismContribution()
    {
        return IdOf(_profile.AllocationMechanism) switch
        {
            var id when ContainsAny(id, "need", "ration") => new PriorityWeights(100, 0, 0, 0.5),
            var id when ContainsAny(id, "market", "price", "wealth") => new PriorityWeights(2, 1, 0, 0),
            var id when ContainsAny(id, "rank", "hierarchy", "patronage") => new PriorityWeights(5, 0, 1, 0.2),
            var id when ContainsAny(id, "equal", "lottery") => new PriorityWeights(10, 0, 0, 0.1),
            _ => new PriorityWeights(25, 0.25, 0.25, 0.25)
        };
    }

    private PriorityWeights DecisionAuthorityContribution()
    {
        return IdOf(_profile.DecisionAuthority) switch
        {
            var id when ContainsAny(id, "participatory", "council", "assembly") => new PriorityWeights(5, 0, 0, 0.3),
            var id when ContainsAny(id, "central", "state", "bureau") => new PriorityWeights(0, 0, 0.2, 0),
            var id when ContainsAny(id, "local", "federated", "delegated") => new PriorityWeights(2, 0, 0, 0.5),
            _ => PriorityWeights.Empty
        };
    }

    private PriorityWeights AccountabilityContribution()
    {
        return IdOf(_profile.AccountabilityMechanism) switch
        {
            var id when ContainsAny(id, "audit", "oversight", "recall") => new PriorityWeights(1, -0.05, -0.05, 0.2),
            var id when ContainsAny(id, "none", "weak") => new PriorityWeights(0, 0.1, 0.1, -0.1),
            _ => PriorityWeights.Empty
        };
    }

    private PriorityWeights InformationFlowContribution()
    {
        return IdOf(_profile.InformationFlow) switch
        {
            var id when ContainsAny(id, "transparent", "open") => new PriorityWeights(10, 0, 0, 0.5),
            var id when ContainsAny(id, "local", "feedback") => new PriorityWeights(5, 0, 0, 0.8),
            var id when ContainsAny(id, "opaque", "restricted") => new PriorityWeights(-5, 0.1, 0.1, -0.2),
            _ => PriorityWeights.Empty
        };
    }

    private PriorityWeights PropertyRegimeContribution()
    {
        return IdOf(_profile.PropertyRegime) switch
        {
            var id when ContainsAny(id, "commons", "communal") => new PriorityWeights(10, 0, 0, 0.3),
            var id when ContainsAny(id, "private", "market") => new PriorityWeights(0, 0.25, 0, 0),
            var id when ContainsAny(id, "state", "public") => new PriorityWeights(3, 0, 0.15, 0),
            _ => PriorityWeights.Empty
        };
    }

    private PriorityWeights AppealProcessContribution()
    {
        return IdOf(_profile.AppealProcess) switch
        {
            var id when ContainsAny(id, "formal", "review", "appeal") => new PriorityWeights(0, 0, 0, 0.8),
            var id when ContainsAny(id, "none", "closed") => new PriorityWeights(0, 0.1, 0.1, -0.2),
            _ => PriorityWeights.Empty
        };
    }

    private IReadOnlyList<InstitutionalAction> BuildInstitutionalActions(WorldState world)
    {
        var population = world.Population.Count;
        var actions = new List<InstitutionalAction>
        {
            new()
            {
                Type = "CompositeGovernanceDecision",
                Description = $"Resolved decisions through {_profile.DecisionAuthority!.DisplayName}.",
                AdministrativeCost = AdministrativeCost(
                    GovernanceDimension.DecisionAuthority,
                    population,
                    DecisionAuthorityDivisor())
            }
        };

        if (!ContainsAny(IdOf(_profile.AccountabilityMechanism), "none", "weak"))
        {
            actions.Add(new InstitutionalAction
            {
                Type = "CompositeGovernanceAccountability",
                Description = $"Applied accountability through {_profile.AccountabilityMechanism!.DisplayName}.",
                AdministrativeCost = AdministrativeCost(
                    GovernanceDimension.AccountabilityMechanism,
                    population,
                    20)
            });
        }

        if (!ContainsAny(IdOf(_profile.AppealProcess), "none", "closed"))
        {
            actions.Add(new InstitutionalAction
            {
                Type = "CompositeGovernanceAppeal",
                Description = $"Processed appeals through {_profile.AppealProcess!.DisplayName}.",
                AdministrativeCost = AdministrativeCost(
                    GovernanceDimension.AppealProcess,
                    population,
                    AppealProcessDivisor())
            });
        }

        return actions;
    }

    private int DecisionAuthorityDivisor()
    {
        return IdOf(_profile.DecisionAuthority) switch
        {
            var id when ContainsAny(id, "participatory", "assembly") => 8,
            var id when ContainsAny(id, "central", "bureau") => 25,
            var id when ContainsAny(id, "local", "federated", "delegated") => 15,
            _ => 20
        };
    }

    private int AppealProcessDivisor()
    {
        return IdOf(_profile.AppealProcess) switch
        {
            var id when ContainsAny(id, "formal", "review") => 25,
            var id when ContainsAny(id, "informal") => 40,
            _ => 30
        };
    }

    private int AdministrativeCost(GovernanceDimension dimension, int population, int divisor)
    {
        var baseCost = Math.Max(1, population / divisor);
        var multiplier = DimensionParameter(dimension, "administrativeCostMultiplier", 1.0);
        return Math.Max(1, (int)Math.Round(baseCost * multiplier, MidpointRounding.AwayFromZero));
    }

    private double CalculatePriority(Citizen citizen, PriorityWeights weights)
    {
        return TotalNeed(citizen) * weights.NeedWeight
            + citizen.Wealth * weights.WealthWeight
            + citizen.SocialPower * weights.RankWeight
            + citizen.Vulnerability * weights.VulnerabilityWeight;
    }

    private double DimensionParameter(GovernanceDimension dimension, string name, double defaultValue)
    {
        return _profile.DimensionParameters.TryGetValue(dimension, out var parameters)
            && parameters.TryGetValue(name, out var value)
            ? value
            : defaultValue;
    }

    private static ModelManifest BuildManifest(
        GovernanceProfile profile,
        IReadOnlyList<string> presetAssumptions,
        IReadOnlyList<string> presetKnownFailureModes)
    {
        var dimensionManifests = profile.Dimensions()
            .Select(dimension => new GovernanceDimensionManifest
            {
                DimensionId = dimension.Dimension.GetId(),
                DimensionName = dimension.Dimension.GetDisplayName(),
                ValueId = dimension.Id,
                ValueName = dimension.DisplayName,
                Description = dimension.Description,
                Assumption = BuildDimensionAssumption(dimension),
                Parameters = profile.DimensionParameters.TryGetValue(dimension.Dimension, out var parameters)
                    ? new Dictionary<string, double>(parameters)
                    : new Dictionary<string, double>(),
                KnownFailureModes = DimensionFailureModes(dimension)
            })
            .ToList();

        var dimensionAssumptions = dimensionManifests
            .Select(dimension => new ModelAssumption
            {
                Name = dimension.DimensionId,
                Default = 1.0,
                Description = $"{dimension.DimensionName}: {dimension.ValueName}. {dimension.Description}".Trim()
            });
        var presetModelAssumptions = presetAssumptions.Select((assumption, index) => new ModelAssumption
        {
            Name = $"preset-assumption-{index + 1}",
            Default = 1.0,
            Description = assumption
        });

        return new ModelManifest
        {
            Model = $"CompositeGovernance:{profile.Id}",
            Version = "0.1.0",
            Description = $"Composite governance model for profile '{profile.Name}'. {profile.Description}".Trim(),
            Assumptions = dimensionAssumptions.Concat(presetModelAssumptions).ToList(),
            GovernanceDimensions = dimensionManifests,
            KnownFailureModes = DistinctPreservingOrder([
                "dimension interactions can amplify unintended priorities",
                "administrative load can rise when accountability or appeals are strong",
                "profile labels are simplified bundles of assumptions",
                .. presetKnownFailureModes,
                .. dimensionManifests.SelectMany(dimension => dimension.KnownFailureModes)
            ])
        };
    }

    private static string BuildDimensionAssumption(GovernanceDimensionValue dimension)
    {
        return dimension.Dimension switch
        {
            GovernanceDimension.AllocationMechanism => $"Allocation priority follows {dimension.DisplayName}.",
            GovernanceDimension.DecisionAuthority => $"Decision authority is exercised through {dimension.DisplayName}.",
            GovernanceDimension.AccountabilityMechanism => $"Accountability is provided by {dimension.DisplayName}.",
            GovernanceDimension.InformationFlow => $"Decision makers observe conditions through {dimension.DisplayName}.",
            GovernanceDimension.PropertyRegime => $"Resource control follows {dimension.DisplayName}.",
            GovernanceDimension.AppealProcess => $"Disputed outcomes are handled through {dimension.DisplayName}.",
            _ => $"{dimension.Dimension.GetDisplayName()} uses {dimension.DisplayName}."
        };
    }

    private static IReadOnlyList<string> DimensionFailureModes(GovernanceDimensionValue dimension)
    {
        var id = dimension.Id;
        return dimension.Dimension switch
        {
            GovernanceDimension.AllocationMechanism when ContainsAny(id, "market", "price", "wealth") =>
            [
                "wealth-weighted allocation can exclude low-wealth citizens during scarcity"
            ],
            GovernanceDimension.AllocationMechanism when ContainsAny(id, "rank", "hierarchy", "patronage") =>
            [
                "rank-weighted allocation can persistently under-serve low-power citizens"
            ],
            GovernanceDimension.AllocationMechanism when ContainsAny(id, "need", "ration") =>
            [
                "need-weighted allocation depends on accurate and timely need information"
            ],
            GovernanceDimension.DecisionAuthority when ContainsAny(id, "participatory", "assembly") =>
            [
                "participatory authority can increase coordination time during fast shocks"
            ],
            GovernanceDimension.DecisionAuthority when ContainsAny(id, "central", "bureau") =>
            [
                "central authority can miss local conditions when feedback is weak"
            ],
            GovernanceDimension.DecisionAuthority when ContainsAny(id, "local", "federated", "delegated") =>
            [
                "federated authority can produce uneven local capacity"
            ],
            GovernanceDimension.AccountabilityMechanism when ContainsAny(id, "none", "weak") =>
            [
                "weak accountability can let allocation failures persist"
            ],
            GovernanceDimension.AccountabilityMechanism when ContainsAny(id, "audit", "oversight", "recall") =>
            [
                "strong accountability can raise administrative load"
            ],
            GovernanceDimension.InformationFlow when ContainsAny(id, "opaque", "restricted") =>
            [
                "restricted information can hide emerging unmet needs"
            ],
            GovernanceDimension.InformationFlow when ContainsAny(id, "transparent", "open", "feedback") =>
            [
                "transparent feedback can increase reporting burden"
            ],
            GovernanceDimension.PropertyRegime when ContainsAny(id, "private", "market") =>
            [
                "private control can concentrate access under unequal wealth"
            ],
            GovernanceDimension.PropertyRegime when ContainsAny(id, "commons", "communal") =>
            [
                "commons pooling can strain coordination when scarcity is acute"
            ],
            GovernanceDimension.PropertyRegime when ContainsAny(id, "state", "public") =>
            [
                "public control can become brittle when planning information is wrong"
            ],
            GovernanceDimension.AppealProcess when ContainsAny(id, "none", "closed") =>
            [
                "closed appeals can leave errors uncorrected"
            ],
            GovernanceDimension.AppealProcess when ContainsAny(id, "formal", "review", "appeal") =>
            [
                "formal appeals can be too slow for urgent needs"
            ],
            GovernanceDimension.AppealProcess when ContainsAny(id, "informal") =>
            [
                "informal appeals can be inconsistent across groups"
            ],
            _ => []
        };
    }

    private static List<string> DistinctPreservingOrder(IEnumerable<string> values)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static string IdOf(GovernanceDimensionValue? value)
    {
        return value?.Id ?? "";
    }

    private static GovernanceProfile GetProfile(GovernancePreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        return preset.Profile;
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record PriorityWeights(
        double NeedWeight,
        double WealthWeight,
        double RankWeight,
        double VulnerabilityWeight)
    {
        public static PriorityWeights Empty { get; } = new(0, 0, 0, 0);

        public PriorityWeights Add(PriorityWeights other)
        {
            return new PriorityWeights(
                NeedWeight + other.NeedWeight,
                WealthWeight + other.WealthWeight,
                RankWeight + other.RankWeight,
                VulnerabilityWeight + other.VulnerabilityWeight);
        }
    }
}
