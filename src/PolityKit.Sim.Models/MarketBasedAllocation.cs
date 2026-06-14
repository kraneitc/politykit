using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Models;

public sealed class MarketBasedAllocation : AllocationModelBase
{
    public override string Name => "MarketBasedAllocation";

    public override ModelManifest Manifest { get; } = new()
    {
        Model = "MarketBasedAllocation",
        Version = "0.1.0",
        Description = "Allocates resources according to wealth and purchasing power, with need as demand pressure.",
        Assumptions =
        [
            new ModelAssumption
            {
                Name = "wealthPriorityWeight",
                Default = 1.0,
                Description = "How strongly wealth affects resource priority."
            },
            new ModelAssumption
            {
                Name = "needDemandWeight",
                Default = 0.2,
                Description = "How strongly unmet need increases demand priority."
            }
        ],
        KnownFailureModes =
        [
            "essential-good exclusion for low-wealth citizens",
            "resource concentration",
            "high inequality during scarcity"
        ]
    };

    public override SystemDecision Decide(WorldState world, Core.Simulation.SystemContext context)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);

        var wealthPriorityWeight = context.Parameters.GetValueOrDefault("wealthPriorityWeight", 1.0);
        var needDemandWeight = context.Parameters.GetValueOrDefault("needDemandWeight", 0.2);
        var decision = new SystemDecision();

        foreach (var citizen in world.Population.Citizens)
        {
            var priority = citizen.Wealth * wealthPriorityWeight
                + TotalNeed(citizen) * 10 * needDemandWeight;

            decision.Allocations.AddRange(AllocateBasicNeeds(
                citizen,
                priority,
                "Prioritized by wealth and demand pressure."));
        }

        decision.InstitutionalActions.Add(new InstitutionalAction
        {
            Type = "MarketAllocationSettlement",
            Description = "Processed allocations through a low-administration market exchange.",
            AdministrativeCost = Math.Max(1, world.Population.Count / 50)
        });

        return decision;
    }
}
