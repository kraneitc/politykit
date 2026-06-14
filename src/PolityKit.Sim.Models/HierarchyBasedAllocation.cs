using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Models;

public sealed class HierarchyBasedAllocation : AllocationModelBase
{
    public override string Name => "HierarchyBasedAllocation";

    public override ModelManifest Manifest { get; } = new()
    {
        Model = "HierarchyBasedAllocation",
        Version = "0.1.0",
        Description = "Allocates resources according to social power, obligation, and visible need.",
        Assumptions =
        [
            new ModelAssumption
            {
                Name = "rankPriorityWeight",
                Default = 0.7,
                Description = "How strongly social power affects resource priority."
            },
            new ModelAssumption
            {
                Name = "obligationNeedWeight",
                Default = 0.3,
                Description = "How strongly visible need tempers rank-based allocation."
            }
        ],
        KnownFailureModes =
        [
            "elite capture",
            "under-provision to low-power citizens",
            "dependence on competent or obligated elites"
        ]
    };

    public override SystemDecision Decide(WorldState world, Core.Simulation.SystemContext context)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);

        var rankPriorityWeight = context.Parameters.GetValueOrDefault("rankPriorityWeight", 0.7);
        var obligationNeedWeight = context.Parameters.GetValueOrDefault("obligationNeedWeight", 0.3);
        var decision = new SystemDecision();

        foreach (var citizen in world.Population.Citizens)
        {
            var priority = citizen.SocialPower * rankPriorityWeight
                + TotalNeed(citizen) * 25 * obligationNeedWeight
                + citizen.Vulnerability * obligationNeedWeight;

            decision.Allocations.AddRange(AllocateBasicNeeds(
                citizen,
                priority,
                "Prioritized by social power, obligation, and visible need."));
        }

        decision.InstitutionalActions.Add(new InstitutionalAction
        {
            Type = "HierarchyAllocationReview",
            Description = "Processed allocations through rank and obligation relationships.",
            AdministrativeCost = Math.Max(1, world.Population.Count / 20)
        });

        return decision;
    }
}
