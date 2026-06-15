using PolityKit.Sim.Core.Models;

namespace PolityKit.Sim.Models;

public static class DefaultModelSet
{
    public static IReadOnlyList<ISystemModel> Create()
    {
        return Create(new GovernancePresetCatalog());
    }

    public static IReadOnlyList<ISystemModel> Create(GovernancePresetCatalog governancePresetCatalog)
    {
        return
        [
            new NeedBasedAllocation(),
            new MarketBasedAllocation(),
            new HierarchyBasedAllocation(),
            .. governancePresetCatalog.All.Select(preset => new CompositeGovernanceModel(preset))
        ];
    }
}
