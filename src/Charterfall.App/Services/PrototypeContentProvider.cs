using Charterfall.App.Models;

namespace Charterfall.App.Services;

public sealed class PrototypeContentProvider : IPrototypeContentProvider
{
    private static readonly PrototypeContent Content = new(
        new SettlementProfile(
            "greywater-compact",
            "Greywater Compact",
            "A flood-cut basin settlement with brittle food stores, limited administrative capacity, and intermittent outside contact."),
        new CrisisCard(
            "failed-harvest",
            "Failed Harvest",
            "village-food-crisis",
            12345,
            120,
            "need-based-allocation placeholder",
            "Pending PolityKit API integration"),
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
}
