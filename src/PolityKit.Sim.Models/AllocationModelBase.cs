using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Models;

public abstract class AllocationModelBase : ISystemModel
{
    public abstract string Name { get; }

    public virtual string Version => "0.1.0";

    public abstract ModelManifest Manifest { get; }

    public abstract SystemDecision Decide(WorldState world, Core.Simulation.SystemContext context);

    protected static IEnumerable<ResourceAllocation> AllocateNeedUnits(
        Citizen citizen,
        ResourceKind resource,
        int need,
        double priority,
        string reason)
    {
        for (var index = 0; index < need; index++)
        {
            yield return new ResourceAllocation
            {
                CitizenId = citizen.Id,
                Resource = resource,
                Amount = 1,
                Priority = priority,
                Reason = reason
            };
        }
    }

    protected static IEnumerable<ResourceAllocation> AllocateBasicNeeds(Citizen citizen, double priority, string reason)
    {
        foreach (var allocation in AllocateNeedUnits(citizen, ResourceKind.Food, citizen.FoodNeed, priority, reason))
        {
            yield return allocation;
        }

        foreach (var allocation in AllocateNeedUnits(citizen, ResourceKind.Medicine, citizen.HealthNeed, priority, reason))
        {
            yield return allocation;
        }

        foreach (var allocation in AllocateNeedUnits(citizen, ResourceKind.Housing, citizen.HousingNeed, priority, reason))
        {
            yield return allocation;
        }
    }

    protected static int TotalNeed(Citizen citizen)
    {
        return citizen.FoodNeed + citizen.HealthNeed + citizen.HousingNeed;
    }
}
