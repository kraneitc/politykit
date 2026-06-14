using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Engine;

public sealed class DefaultWorldRule : IWorldRule
{
    public void Apply(WorldState world, SystemDecision decision)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(decision);

        ApplyAllocations(world, decision);
        ApplyPolicyChanges(world, decision);
        ApplyInstitutionalActions(world, decision);
        ApplyUnmetNeedEffects(world);
        ApplyAdministrativeEffects(world);
    }

    private static void ApplyAllocations(WorldState world, SystemDecision decision)
    {
        foreach (var allocation in decision.Allocations.OrderByDescending(allocation => allocation.Priority))
        {
            if (allocation.Amount <= 0 || allocation.CitizenId is null)
            {
                continue;
            }

            var citizen = world.Population.Citizens.FirstOrDefault(candidate => candidate.Id == allocation.CitizenId.Value);
            if (citizen is null || !world.Resources.TryConsume(allocation.Resource, allocation.Amount))
            {
                continue;
            }

            ReduceNeed(citizen, allocation.Resource, allocation.Amount);

            world.Events.Add(new SimulationEvent
            {
                Tick = world.Tick,
                Type = "ResourceAllocated",
                Description = $"Allocated {allocation.Amount} {allocation.Resource} to citizen {citizen.Id}.",
                Data =
                {
                    ["citizenId"] = citizen.Id,
                    ["resource"] = allocation.Resource.ToString(),
                    ["amount"] = allocation.Amount,
                    ["reason"] = allocation.Reason
                }
            });
        }
    }

    private static void ApplyPolicyChanges(WorldState world, SystemDecision decision)
    {
        foreach (var policyChange in decision.PolicyChanges)
        {
            world.Events.Add(new SimulationEvent
            {
                Tick = world.Tick,
                Type = "PolicyChanged",
                Description = $"Policy '{policyChange.Name}' changed to {policyChange.Value}.",
                Data =
                {
                    ["name"] = policyChange.Name,
                    ["value"] = policyChange.Value,
                    ["reason"] = policyChange.Reason
                }
            });
        }
    }

    private static void ApplyInstitutionalActions(WorldState world, SystemDecision decision)
    {
        foreach (var action in decision.InstitutionalActions)
        {
            world.Institutions.AdministrativeLoad += Math.Max(0, action.AdministrativeCost);
            world.Events.Add(new SimulationEvent
            {
                Tick = world.Tick,
                Type = action.Type,
                Description = action.Description,
                Data =
                {
                    ["administrativeCost"] = action.AdministrativeCost
                }
            });
        }
    }

    private static void ApplyUnmetNeedEffects(WorldState world)
    {
        var unmetNeed = world.Population.Citizens.Sum(citizen => citizen.FoodNeed + citizen.HealthNeed + citizen.HousingNeed);
        if (unmetNeed == 0)
        {
            return;
        }

        var trustPenalty = Math.Min(10, Math.Max(1, unmetNeed / Math.Max(1, world.Population.Count)));
        world.Institutions.Trust = Math.Max(0, world.Institutions.Trust - trustPenalty);

        foreach (var citizen in world.Population.Citizens)
        {
            if (citizen.FoodNeed + citizen.HealthNeed + citizen.HousingNeed > 0)
            {
                citizen.TrustInSystem = Math.Max(0, citizen.TrustInSystem - trustPenalty);
            }
        }

        world.Events.Add(new SimulationEvent
        {
            Tick = world.Tick,
            Type = "UnmetNeeds",
            Description = $"Unmet need total was {unmetNeed}; institutional trust fell by {trustPenalty}.",
            Data =
            {
                ["unmetNeed"] = unmetNeed,
                ["trustPenalty"] = trustPenalty
            }
        });
    }

    private static void ApplyAdministrativeEffects(WorldState world)
    {
        var capacity = Math.Max(world.Institutions.AdministrativeCapacity, world.Resources.AdminCapacity);
        if (world.Institutions.AdministrativeLoad > capacity)
        {
            var overflow = world.Institutions.AdministrativeLoad - capacity;
            world.Institutions.AppealBacklog += overflow;
            world.Institutions.Trust = Math.Max(0, world.Institutions.Trust - Math.Min(5, overflow));

            world.Events.Add(new SimulationEvent
            {
                Tick = world.Tick,
                Type = "AdministrativeBacklog",
                Description = $"Administrative load exceeded capacity by {overflow}.",
                Data =
                {
                    ["capacity"] = capacity,
                    ["load"] = world.Institutions.AdministrativeLoad,
                    ["overflow"] = overflow
                }
            });
        }

        world.Institutions.AdministrativeLoad = 0;
    }

    private static void ReduceNeed(Citizen citizen, ResourceKind resource, int amount)
    {
        switch (resource)
        {
            case ResourceKind.Food:
                citizen.FoodNeed = Math.Max(0, citizen.FoodNeed - amount);
                break;
            case ResourceKind.Medicine:
                citizen.HealthNeed = Math.Max(0, citizen.HealthNeed - amount);
                break;
            case ResourceKind.Housing:
                citizen.HousingNeed = Math.Max(0, citizen.HousingNeed - amount);
                break;
        }
    }
}
