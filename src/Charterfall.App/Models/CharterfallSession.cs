namespace Charterfall.App.Models;

public sealed class CharterfallSession
{
    public string SettlementId { get; set; } = "greywater-compact";

    public int ChapterNumber { get; set; } = 1;

    public string ActiveCrisisId { get; set; } = "failed-harvest";

    public string ScenarioSource { get; set; } = "village-food-crisis";

    public int Seed { get; set; } = 12345;

    public int Ticks { get; set; } = 120;

    public string AssumptionsSummary { get; set; } = "Greywater Compact / Failed Harvest / village-food-crisis / seed 12345 / 120 ticks";

    public List<string> SelectedClauseIds { get; } =
    [
        "allocation.need_based",
        "transparency.public_ledger",
        "accountability.appeal_board",
        "emergency.none"
    ];

    public List<string> AuthoritativeModelIds { get; } = ["need-based-allocation"];

    public Dictionary<string, double> AuthoritativeParameters { get; } = new()
    {
        ["needPriorityWeight"] = 1.0,
        ["vulnerabilityPriorityWeight"] = 0.5
    };

    public List<string> GameLayerClauseIds { get; } = ["emergency.none"];

    public List<string> ClauseSelectionErrors { get; } = [];

    public string CharterSummary { get; set; } =
        "Need-Based Allocation, Public Ledger, Appeal Board, No Emergency Powers";

    public List<string> AuthoritativeRunIds { get; } = [];

    public string? SelectedContinuationRunId { get; set; }

    public string CompactInquirySummary { get; set; } = string.Empty;

    public string? LastError { get; set; }

    public bool IsBusy { get; set; }

    public bool HasViewedInquiry { get; set; }

    public List<PrototypeRunRecord> RunHistory { get; } = [];
}
