using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Models;

public sealed class NeedBasedAllocation : AllocationModelBase
{
    public override string Name => "NeedBasedAllocation";

    public override ModelManifest Manifest { get; } = new()
    {
        Model = "NeedBasedAllocation",
        Version = "0.1.0",
        Description = "Allocates resources according to need, vulnerability, and urgency.",
        Assumptions =
        [
            new ModelAssumption
            {
                Name = "needPriorityWeight",
                Default = 1.0,
                Description = "How strongly unmet need affects resource priority."
            },
            new ModelAssumption
            {
                Name = "vulnerabilityPriorityWeight",
                Default = 0.5,
                Description = "How strongly vulnerability affects resource priority."
            }
        ],
        KnownFailureModes =
        [
            "high assessment overhead",
            "administrative backlog",
            "dependence on accurate need assessment"
        ]
    };

    public override SystemDecision Decide(WorldState world, Core.Simulation.SystemContext context)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);

        var needPriorityWeight = context.Parameters.GetValueOrDefault("needPriorityWeight", 1.0);
        var vulnerabilityPriorityWeight = context.Parameters.GetValueOrDefault("vulnerabilityPriorityWeight", 0.5);
        var decision = new SystemDecision();

        foreach (var citizen in world.Population.Citizens)
        {
            var priority = TotalNeed(citizen) * 100 * needPriorityWeight
                + citizen.Vulnerability * vulnerabilityPriorityWeight;

            decision.Allocations.AddRange(AllocateBasicNeeds(
                citizen,
                priority,
                "Prioritized by unmet need and vulnerability."));
        }

        decision.InstitutionalActions.Add(new InstitutionalAction
        {
            Type = "NeedAssessment",
            Description = "Assessed citizen need and vulnerability before allocation.",
            AdministrativeCost = Math.Max(1, world.Population.Count / 10)
        });

        return decision;
    }
}
