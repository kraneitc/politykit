using System.Text;
using PolityKit.Sim.Core.Models.Governance;

namespace PolityKit.Sim.Models;

public sealed class GovernancePresetCatalog
{
    private readonly IReadOnlyList<GovernancePreset> _presets;

    public GovernancePresetCatalog()
        : this(CreateDefaults())
    {
    }

    public GovernancePresetCatalog(IEnumerable<GovernancePreset> presets)
    {
        ArgumentNullException.ThrowIfNull(presets);
        _presets = presets.ToArray();
    }

    public IReadOnlyList<GovernancePreset> All => _presets;

    public GovernancePreset? FindById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return _presets.FirstOrDefault(preset =>
            string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public GovernancePreset? FindByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _presets.FirstOrDefault(preset =>
            string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ToKebabCase(preset.Name), name, StringComparison.OrdinalIgnoreCase));
    }

    public GovernancePreset? Find(string idOrName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idOrName);

        return FindById(idOrName) ?? FindByName(idOrName);
    }

    private static IReadOnlyList<GovernancePreset> CreateDefaults()
    {
        return
        [
            Preset(
                "participatory-commons",
                "Participatory Commons",
                "A commons-oriented profile where allocation is need-weighted and decisions are made through participatory assemblies.",
                [
                    "community assemblies are able to identify urgent needs",
                    "shared property rules make basic-resource access easier to coordinate",
                    "open information and recall procedures make allocation decisions contestable"
                ],
                [
                    "participatory processes can add administrative load under time pressure",
                    "slow consensus can delay response to fast-moving shortages",
                    "local majority preferences can still miss marginal needs"
                ],
                Value(GovernanceDimension.AllocationMechanism, "need-weighted-rationing", "Need-Weighted Rationing", "Scarce goods are prioritized by unmet need."),
                Value(GovernanceDimension.DecisionAuthority, "participatory-assembly", "Participatory Assembly", "Residents deliberate through an open assembly process."),
                Value(GovernanceDimension.AccountabilityMechanism, "recall-and-audit", "Recall And Audit", "Decision makers can be recalled and allocation records are audited."),
                Value(GovernanceDimension.InformationFlow, "transparent-local-feedback", "Transparent Local Feedback", "Local reports and allocation outcomes are visible to participants."),
                Value(GovernanceDimension.PropertyRegime, "commons-stewardship", "Commons Stewardship", "Essential resources are managed as shared commons."),
                Value(GovernanceDimension.AppealProcess, "formal-community-review", "Formal Community Review", "Disputed decisions can be reviewed by a community panel."),
                new Dictionary<GovernanceDimension, Dictionary<string, double>>
                {
                    [GovernanceDimension.DecisionAuthority] = new() { ["administrativeCostMultiplier"] = 1.15 },
                    [GovernanceDimension.AccountabilityMechanism] = new() { ["administrativeCostMultiplier"] = 1.1 }
                }),
            Preset(
                "regulated-market",
                "Regulated Market",
                "A market-oriented profile where prices guide allocation but oversight and appeals constrain extreme outcomes.",
                [
                    "price signals carry useful information about scarcity",
                    "oversight can reduce exclusion from purely wealth-weighted allocation",
                    "private property rules are bounded by formal review"
                ],
                [
                    "wealth differences can still dominate access when scarcity is severe",
                    "regulatory lag can miss sudden failures",
                    "formal appeals can be too slow for urgent needs"
                ],
                Value(GovernanceDimension.AllocationMechanism, "market-price-weighted", "Market Price Weighted", "Access is shaped by wealth and price-like scarcity signals."),
                Value(GovernanceDimension.DecisionAuthority, "regulated-market-authority", "Regulated Market Authority", "Market exchange is supervised by a bounded regulator."),
                Value(GovernanceDimension.AccountabilityMechanism, "oversight-audit", "Oversight Audit", "Regulators audit allocation outcomes for visible failures."),
                Value(GovernanceDimension.InformationFlow, "market-and-public-reporting", "Market And Public Reporting", "Market signals are paired with public reporting."),
                Value(GovernanceDimension.PropertyRegime, "private-property-with-public-duties", "Private Property With Public Duties", "Private control is constrained by public-interest duties."),
                Value(GovernanceDimension.AppealProcess, "formal-regulatory-appeal", "Formal Regulatory Appeal", "Participants can contest decisions through a regulator."),
                new Dictionary<GovernanceDimension, Dictionary<string, double>>
                {
                    [GovernanceDimension.AppealProcess] = new() { ["administrativeCostMultiplier"] = 1.2 }
                }),
            Preset(
                "central-planning",
                "Central Planning",
                "A state-planning profile where central bureaus use need records to ration essential resources.",
                [
                    "central planners can collect sufficient need information",
                    "public property rules let planners redirect resources quickly",
                    "formal audits can detect broad allocation failures"
                ],
                [
                    "restricted feedback can hide local emergencies",
                    "central queues can favor ranked or visible groups",
                    "large plans can become brittle when shocks differ from forecasts"
                ],
                Value(GovernanceDimension.AllocationMechanism, "need-weighted-state-rationing", "Need-Weighted State Rationing", "Rationing is centrally assigned using recorded need."),
                Value(GovernanceDimension.DecisionAuthority, "central-bureau", "Central Bureau", "A central planning bureau sets allocation priorities."),
                Value(GovernanceDimension.AccountabilityMechanism, "state-audit", "State Audit", "Internal audits review plan compliance."),
                Value(GovernanceDimension.InformationFlow, "restricted-planning-reports", "Restricted Planning Reports", "Information flows through official reporting channels."),
                Value(GovernanceDimension.PropertyRegime, "state-public-ownership", "State Public Ownership", "Essential resources are treated as public assets."),
                Value(GovernanceDimension.AppealProcess, "formal-bureau-review", "Formal Bureau Review", "Appeals move through a formal administrative review."),
                new Dictionary<GovernanceDimension, Dictionary<string, double>>
                {
                    [GovernanceDimension.DecisionAuthority] = new() { ["administrativeCostMultiplier"] = 0.9 },
                    [GovernanceDimension.AppealProcess] = new() { ["administrativeCostMultiplier"] = 1.25 }
                }),
            Preset(
                "patronage-hierarchy",
                "Patronage Hierarchy",
                "A rank-oriented profile where allocation follows social power and accountability is weak.",
                [
                    "higher-ranked actors can mobilize distribution networks",
                    "client ties can move resources without formal review",
                    "opaque information makes status and access mutually reinforcing"
                ],
                [
                    "low-power residents can be persistently under-served",
                    "weak accountability can normalize severe unmet need",
                    "opaque decisions can reduce trust during shortages"
                ],
                Value(GovernanceDimension.AllocationMechanism, "rank-patronage-allocation", "Rank Patronage Allocation", "Allocation priority follows status and patron-client ties."),
                Value(GovernanceDimension.DecisionAuthority, "hierarchy-patronage-council", "Hierarchy Patronage Council", "Senior patrons arbitrate access."),
                Value(GovernanceDimension.AccountabilityMechanism, "weak-informal-accountability", "Weak Informal Accountability", "Accountability depends on informal pressure."),
                Value(GovernanceDimension.InformationFlow, "opaque-restricted-information", "Opaque Restricted Information", "Allocation information is limited and status-mediated."),
                Value(GovernanceDimension.PropertyRegime, "private-patronage-control", "Private Patronage Control", "Resource control is concentrated in patron networks."),
                Value(GovernanceDimension.AppealProcess, "closed-patron-appeal", "Closed Patron Appeal", "Appeals depend on patron access rather than formal review."),
                new Dictionary<GovernanceDimension, Dictionary<string, double>>
                {
                    [GovernanceDimension.DecisionAuthority] = new() { ["administrativeCostMultiplier"] = 0.75 }
                }),
            Preset(
                "mutual-aid-federation",
                "Mutual Aid Federation",
                "A federated local profile where autonomous groups coordinate need-weighted support through shared feedback.",
                [
                    "local groups can observe vulnerability faster than central actors",
                    "federated coordination can pool resources without fully centralizing control",
                    "informal review is strongest when groups maintain open feedback channels"
                ],
                [
                    "coordination costs can rise across many local groups",
                    "uneven local capacity can create patchy coverage",
                    "informal appeals may struggle with persistent conflicts"
                ],
                Value(GovernanceDimension.AllocationMechanism, "need-weighted-mutual-aid", "Need-Weighted Mutual Aid", "Mutual aid groups prioritize urgent needs."),
                Value(GovernanceDimension.DecisionAuthority, "local-federated-delegation", "Local Federated Delegation", "Local groups coordinate through delegates."),
                Value(GovernanceDimension.AccountabilityMechanism, "peer-recall-and-audit", "Peer Recall And Audit", "Peer groups can recall delegates and audit allocations."),
                Value(GovernanceDimension.InformationFlow, "local-feedback-network", "Local Feedback Network", "Needs and offers circulate through local feedback channels."),
                Value(GovernanceDimension.PropertyRegime, "communal-commons-pooling", "Communal Commons Pooling", "Resources are pooled through communal agreements."),
                Value(GovernanceDimension.AppealProcess, "informal-peer-review", "Informal Peer Review", "Disputes are reviewed through peer mediation."),
                new Dictionary<GovernanceDimension, Dictionary<string, double>>
                {
                    [GovernanceDimension.DecisionAuthority] = new() { ["administrativeCostMultiplier"] = 1.05 },
                    [GovernanceDimension.AppealProcess] = new() { ["administrativeCostMultiplier"] = 0.9 }
                }),
            Preset(
                "technocratic-administration",
                "Technocratic Administration",
                "A rules-and-audit profile where professional administrators use transparent indicators and formal reviews.",
                [
                    "expert rules can turn need observations into consistent allocation decisions",
                    "transparent indicators reduce arbitrary decision variation",
                    "formal review can correct some administrative errors"
                ],
                [
                    "metric choices can miss lived vulnerability",
                    "formal processes can generate administrative burden",
                    "expert rules can become brittle when conditions change"
                ],
                Value(GovernanceDimension.AllocationMechanism, "need-and-risk-scoring", "Need And Risk Scoring", "Allocation is based on measured need and risk indicators."),
                Value(GovernanceDimension.DecisionAuthority, "bureau-technocratic-administration", "Technocratic Administration", "Professional administrators apply formal rules."),
                Value(GovernanceDimension.AccountabilityMechanism, "oversight-audit", "Oversight Audit", "Independent oversight audits rule application."),
                Value(GovernanceDimension.InformationFlow, "transparent-indicator-reporting", "Transparent Indicator Reporting", "Indicators and decisions are published for review."),
                Value(GovernanceDimension.PropertyRegime, "mixed-public-administration", "Mixed Public Administration", "Public administration coordinates across mixed ownership."),
                Value(GovernanceDimension.AppealProcess, "formal-administrative-review", "Formal Administrative Review", "Cases can be appealed through a formal administrative process."),
                new Dictionary<GovernanceDimension, Dictionary<string, double>>
                {
                    [GovernanceDimension.AccountabilityMechanism] = new() { ["administrativeCostMultiplier"] = 1.15 },
                    [GovernanceDimension.AppealProcess] = new() { ["administrativeCostMultiplier"] = 1.3 }
                })
        ];
    }

    private static GovernancePreset Preset(
        string id,
        string name,
        string description,
        IReadOnlyList<string> assumptions,
        IReadOnlyList<string> knownFailureModes,
        GovernanceDimensionValue allocationMechanism,
        GovernanceDimensionValue decisionAuthority,
        GovernanceDimensionValue accountabilityMechanism,
        GovernanceDimensionValue informationFlow,
        GovernanceDimensionValue propertyRegime,
        GovernanceDimensionValue appealProcess,
        Dictionary<GovernanceDimension, Dictionary<string, double>>? dimensionParameters = null)
    {
        return new GovernancePreset
        {
            Id = id,
            Name = name,
            Description = description,
            Assumptions = assumptions,
            KnownFailureModes = knownFailureModes,
            Profile = new GovernanceProfile
            {
                Id = id,
                Name = name,
                Description = description,
                AllocationMechanism = allocationMechanism,
                DecisionAuthority = decisionAuthority,
                AccountabilityMechanism = accountabilityMechanism,
                InformationFlow = informationFlow,
                PropertyRegime = propertyRegime,
                AppealProcess = appealProcess,
                DimensionParameters = dimensionParameters ?? []
            }
        };
    }

    private static GovernanceDimensionValue Value(
        GovernanceDimension dimension,
        string id,
        string displayName,
        string description)
    {
        return new GovernanceDimensionValue(dimension, id, displayName, description);
    }

    private static string ToKebabCase(string value)
    {
        var builder = new StringBuilder();
        var previousWasSeparator = false;

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return previousWasSeparator
            ? builder.ToString(0, builder.Length - 1)
            : builder.ToString();
    }
}
