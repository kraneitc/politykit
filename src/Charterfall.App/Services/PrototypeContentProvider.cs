using Charterfall.App.Models;

namespace Charterfall.App.Services;

public sealed class PrototypeContentProvider : IPrototypeContentProvider
{
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

    private static readonly PrototypeContent Content = new(
        new SettlementProfile(
            "greywater-compact",
            "Greywater Compact",
            "A flood-cut basin settlement with brittle food stores, limited administrative capacity, and intermittent outside contact."),
        CampaignCrises[0],
        CampaignCrises,
        [
            new CharterClause(
                "allocation.need_based",
                "Need-Based Allocation",
                "Prioritize residents with the greatest unmet need while tracking administrative load.",
                true)
        ],
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
}
