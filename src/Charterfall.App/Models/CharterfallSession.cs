namespace Charterfall.App.Models;

public sealed class CharterfallSession
{
    public string SettlementId { get; set; } = "greywater-compact";

    public int ChapterNumber { get; set; } = 1;

    public string ActiveCrisisId { get; set; } = "failed-harvest";

    public List<string> SelectedClauseIds { get; } = ["allocation.need_based"];

    public List<string> AuthoritativeRunIds { get; } = [];

    public string? SelectedContinuationRunId { get; set; }

    public string CompactInquirySummary { get; set; } = string.Empty;

    public string? LastError { get; set; }

    public bool IsBusy { get; set; }

    public bool HasViewedInquiry { get; set; }

    public List<PrototypeRunRecord> RunHistory { get; } = [];
}
