using PolityKit.Sim.Core.Models;

namespace PolityKit.Sim.Models;

public static class DefaultModelSet
{
    public static IReadOnlyList<ISystemModel> Create()
    {
        return
        [
            new NeedBasedAllocation(),
            new MarketBasedAllocation(),
            new HierarchyBasedAllocation()
        ];
    }
}
