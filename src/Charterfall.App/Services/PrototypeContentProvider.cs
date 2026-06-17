using Charterfall.App.Models;

namespace Charterfall.App.Services;

public sealed class PrototypeContentProvider : IPrototypeContentProvider
{
    private const string RunModel = "run-model";
    private const string PresetDimension = "preset-dimension";
    private const string GameLayerOnly = "game-layer-only";

    private static readonly IReadOnlyList<CrisisCard> CampaignCrises =
    [
        new CrisisCard(
            "failed-harvest",
            1,
            "Failed Harvest",
            "Food stores are brittle after a poor growing season, and Greywater must decide who receives scarce supplies first.",
            "village-food-crisis",
            12345,
            120,
            "Tests allocation pressure and visible unmet need.",
            ["Allocation", "transparency", "administrative load", "emergency powers"],
            IsUnlocked: true,
            IsIntegrationAvailable: true,
            "Available for first run path"),
        new CrisisCard(
            "fever-season",
            2,
            "Fever Season",
            "The clinic faces triage pressure as illness spreads through households that already depend on uneven access to care.",
            "examples/medicine-shortage.json",
            24680,
            90,
            "Tests vulnerability, triage, and trust.",
            ["Allocation", "decision authority", "accountability", "appeal board", "expert office"],
            IsUnlocked: false,
            IsIntegrationAvailable: false,
            "Integration pending: campaign card only"),
        new CrisisCard(
            "supply-office-scandal",
            3,
            "Supply Office Scandal",
            "Ledger gaps and favoritism claims put the supply office under pressure before the next relief wagon arrives.",
            "examples/corruption-stress.json",
            98765,
            80,
            "Tests transparency, accountability, legitimacy, and power incentives.",
            ["Transparency", "accountability", "audit office", "citizen review", "emergency powers"],
            IsUnlocked: false,
            IsIntegrationAvailable: false,
            "Integration pending: campaign card only")
    ];

    private static readonly IReadOnlyList<CharterClauseDefinition> ClauseDefinitions =
    [
        new CharterClauseDefinition(
            "allocation.need_based",
            "allocation_method",
            "Need-Based Allocation",
            "Prioritize citizens with the highest unmet need.",
            "Can protect vulnerable citizens, but may increase administrative load and depend on accurate assessment.",
            RunModel,
            "need-based-allocation",
            new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 1.0,
                ["vulnerabilityPriorityWeight"] = 0.5
            },
            ["Highlights unmet need and vulnerability in inquiry copy."],
            "first-run",
            "Represents a simplified crisis rule, not a claim about any real institution.",
            []),
        new CharterClauseDefinition(
            "allocation.market_based",
            "allocation_method",
            "Market-Based Allocation",
            "Let price-like scarcity signals and ability to pay shape access.",
            "Can move resources with low administrative burden, but may leave vulnerable citizens exposed.",
            RunModel,
            "market-based-allocation",
            new Dictionary<string, double>(),
            ["Highlights exclusion risk and wealth-mediated access in inquiry copy."],
            "first-run",
            "Represents a fictional market-like crisis rule under declared assumptions.",
            []),
        new CharterClauseDefinition(
            "allocation.hierarchy_based",
            "allocation_method",
            "Hierarchy-Based Allocation",
            "Give priority to rank, office, or command position.",
            "Can be fast and legible in emergencies, but may normalize unequal protection.",
            RunModel,
            "hierarchy-based-allocation",
            new Dictionary<string, double>(),
            ["Highlights rank, patronage, and ignored low-power groups in inquiry copy."],
            "first-run",
            "Represents a fictional hierarchy rule under declared assumptions.",
            ["capture"]),
        new CharterClauseDefinition(
            "transparency.public_ledger",
            "transparency",
            "Public Ledger",
            "Allocation decisions and shortages are visible to the settlement.",
            "Can build trust and reveal failure, but increases administrative work and political exposure.",
            PresetDimension,
            null,
            new Dictionary<string, double>(),
            ["Shows public ledger language in inquiry and comparison screens."],
            "first-run",
            "Fictional transparency rule under declared assumptions.",
            []),
        new CharterClauseDefinition(
            "transparency.delayed_reporting",
            "transparency",
            "Delayed Reporting",
            "Reports are published after crisis actions are complete.",
            "Can reduce immediate burden, but delays correction and public understanding.",
            GameLayerOnly,
            null,
            new Dictionary<string, double>(),
            ["Delays some explanatory copy until inquiry phase."],
            "first-run",
            "Fictional reporting rule only.",
            ["opacity"]),
        new CharterClauseDefinition(
            "accountability.appeal_board",
            "accountability",
            "Appeal Board",
            "Citizens can challenge allocation decisions through a formal appeal.",
            "Can correct errors, but may be too slow for urgent needs.",
            PresetDimension,
            null,
            new Dictionary<string, double>(),
            ["Adds appeal events and testimony prompts when supported by run outputs."],
            "first-run",
            "Fictional appeal process under declared assumptions.",
            []),
        new CharterClauseDefinition(
            "emergency.none",
            "emergency_powers",
            "No Emergency Powers",
            "Ordinary rules remain in force during acute danger.",
            "Can protect process and trust, but may delay urgent action.",
            GameLayerOnly,
            null,
            new Dictionary<string, double>(),
            ["Restricts emergency-action copy and unlocks process-protection reactions."],
            "first-run",
            "Fictional crisis rule only.",
            []),
        new CharterClauseDefinition(
            "emergency.limited",
            "emergency_powers",
            "Limited Emergency Powers",
            "A narrow set of procedures can be bypassed for one crisis response.",
            "Can address urgent danger, but may create accountability pressure.",
            GameLayerOnly,
            null,
            new Dictionary<string, double>(),
            ["Enables one authored emergency beat or badge."],
            "first-run",
            "Fictional crisis rule only.",
            ["precedent", "accountability-debt"])
    ];

    private static readonly PrototypeContent Content = new(
        new SettlementProfile(
            "greywater-compact",
            "Greywater Compact",
            "A flood-cut basin settlement with brittle food stores, limited administrative capacity, and intermittent outside contact."),
        CampaignCrises[0],
        CampaignCrises,
        ClauseDefinitions,
        [
            new MetricPlaceholder("needs-met", "Needs Met"),
            new MetricPlaceholder("severe-failures", "Severe Failures"),
            new MetricPlaceholder("trust", "Trust"),
            new MetricPlaceholder("inequality", "Inequality"),
            new MetricPlaceholder("administrative-load", "Administrative Load")
        ],
        "Charterfall shows how fictional institutional rules behave inside declared simulation assumptions. It does not prove that a real-world political, economic, or social system is superior.");

    public PrototypeContent GetInitialContent() => Content;

    public IReadOnlyList<CrisisCard> GetCampaignCrises() => CampaignCrises;

    public CrisisCard GetActiveCrisis(int chapterNumber) =>
        CampaignCrises.FirstOrDefault(crisis => crisis.ChapterNumber == chapterNumber)
        ?? throw new InvalidOperationException($"No Charterfall crisis card is configured for chapter {chapterNumber}.");

    public CrisisCard? FindCrisis(string crisisId) =>
        CampaignCrises.FirstOrDefault(crisis => crisis.Id == crisisId);

    public IReadOnlyList<CharterClauseDefinition> GetClauses() => ClauseDefinitions;

    public IReadOnlyList<CharterClauseDefinition> GetClausesForDimension(string dimension) =>
        ClauseDefinitions
            .Where(clause => string.Equals(clause.Dimension, dimension, StringComparison.Ordinal))
            .ToArray();

    public CharterClauseDefinition? FindClause(string clauseId) =>
        ClauseDefinitions.FirstOrDefault(clause => string.Equals(clause.Id, clauseId, StringComparison.Ordinal));

    public CharterRunInputPreview BuildRunInputPreview(IReadOnlyList<string> selectedClauseIds)
    {
        var selected = selectedClauseIds
            .Select(FindClause)
            .OfType<CharterClauseDefinition>()
            .ToArray();
        var models = selected
            .Where(clause => clause.MappingStatus == RunModel && clause.ModelId is not null)
            .Select(clause => clause.ModelId!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var parameters = selected
            .SelectMany(clause => clause.Parameters)
            .GroupBy(parameter => parameter.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);
        var gameLayerOnly = selected
            .Where(clause => clause.MappingStatus == GameLayerOnly)
            .Select(clause => clause.Id)
            .ToArray();
        var presetDimensions = selected
            .Where(clause => clause.MappingStatus == PresetDimension)
            .Select(clause => clause.Id)
            .ToArray();

        return new CharterRunInputPreview(models, parameters, gameLayerOnly, presetDimensions);
    }

    public ClauseSelectionValidationResult ValidateClauseSelection(IReadOnlyList<string> selectedClauseIds)
    {
        var errors = new List<string>();
        var selected = selectedClauseIds
            .Select(id => new { Id = id, Clause = FindClause(id) })
            .ToArray();

        foreach (var missing in selected.Where(item => item.Clause is null))
        {
            errors.Add($"Unknown clause selected: {missing.Id}.");
        }

        var knownClauses = selected.Select(item => item.Clause).OfType<CharterClauseDefinition>().ToArray();
        var allocationCount = knownClauses.Count(clause => clause.Dimension == "allocation_method");
        if (allocationCount == 0)
        {
            errors.Add("Choose one allocation method before resolving the crisis.");
        }
        else if (allocationCount > 1)
        {
            errors.Add("Choose only one allocation method before resolving the crisis.");
        }

        if (knownClauses.All(clause => clause.MappingStatus != RunModel))
        {
            errors.Add("At least one simulation-active clause is required for the future PolityKit run input.");
        }

        foreach (var unavailable in knownClauses.Where(clause => clause.Availability != "first-run"))
        {
            errors.Add($"{unavailable.DisplayName} is not available for the first prototype run.");
        }

        return new ClauseSelectionValidationResult(errors.Count == 0, errors);
    }
}
