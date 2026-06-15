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
            if (citizen is null)
            {
                continue;
            }

            var amount = Math.Min(allocation.Amount, NeedForResource(citizen, allocation.Resource));
            if (amount <= 0 || !world.Resources.TryConsume(allocation.Resource, amount))
            {
                continue;
            }

            var resourceBefore = world.Resources.Get(allocation.Resource) + amount;
            var needBefore = NeedForResource(citizen, allocation.Resource);
            var totalNeedBefore = TotalNeed(citizen);
            ReduceNeed(citizen, allocation.Resource, amount);
            var needAfter = NeedForResource(citizen, allocation.Resource);
            var totalNeedAfter = TotalNeed(citizen);

            world.Events.Add(new SimulationEvent
            {
                Tick = world.Tick,
                Type = "ResourceAllocated",
                Description = $"Allocated {amount} {allocation.Resource} to citizen {citizen.Id}.",
                Data =
                {
                    ["citizenId"] = citizen.Id,
                    ["affectedResource"] = allocation.Resource.ToString(),
                    ["resource"] = allocation.Resource.ToString(),
                    ["amount"] = amount,
                    ["priority"] = allocation.Priority,
                    ["resourceBefore"] = resourceBefore,
                    ["resourceAfter"] = world.Resources.Get(allocation.Resource),
                    ["resourceDelta"] = world.Resources.Get(allocation.Resource) - resourceBefore,
                    ["needBefore"] = needBefore,
                    ["needAfter"] = needAfter,
                    ["needDelta"] = needAfter - needBefore,
                    ["citizenTotalNeedBefore"] = totalNeedBefore,
                    ["citizenTotalNeedAfter"] = totalNeedAfter,
                    ["citizenTotalNeedDelta"] = totalNeedAfter - totalNeedBefore,
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
            var administrativeLoadBefore = world.Institutions.AdministrativeLoad;
            world.Institutions.AdministrativeLoad += Math.Max(0, action.AdministrativeCost);
            world.Events.Add(new SimulationEvent
            {
                Tick = world.Tick,
                Type = action.Type,
                Description = action.Description,
                Data =
                {
                    ["actionType"] = action.Type,
                    ["administrativeCost"] = action.AdministrativeCost,
                    ["administrativeLoadBefore"] = administrativeLoadBefore,
                    ["administrativeLoadAfter"] = world.Institutions.AdministrativeLoad,
                    ["administrativeLoadDelta"] = world.Institutions.AdministrativeLoad - administrativeLoadBefore
                }
            });
        }
    }

    private static void ApplyUnmetNeedEffects(WorldState world)
    {
        var citizensWithUnmetNeed = world.Population.Citizens
            .Where(citizen => citizen.FoodNeed + citizen.HealthNeed + citizen.HousingNeed > 0)
            .ToArray();
        var unmetNeed = citizensWithUnmetNeed.Sum(citizen => citizen.FoodNeed + citizen.HealthNeed + citizen.HousingNeed);
        if (unmetNeed == 0)
        {
            return;
        }

        var trustPenalty = Math.Min(10, Math.Max(1, unmetNeed / Math.Max(1, world.Population.Count)));
        var institutionalTrustBefore = world.Institutions.Trust;
        world.Institutions.Trust = Math.Max(0, world.Institutions.Trust - trustPenalty);

        foreach (var citizen in citizensWithUnmetNeed)
        {
            citizen.TrustInSystem = Math.Max(0, citizen.TrustInSystem - trustPenalty);
        }

        world.Events.Add(new SimulationEvent
        {
            Tick = world.Tick,
            Type = "UnmetNeeds",
            Description = $"Unmet need total was {unmetNeed}; institutional trust fell by {trustPenalty}.",
            Data =
            {
                ["unmetNeed"] = unmetNeed,
                ["affectedCitizenCount"] = citizensWithUnmetNeed.Length,
                ["populationCount"] = world.Population.Count,
                ["averageUnmetNeed"] = world.Population.Count == 0 ? 0 : (double)unmetNeed / world.Population.Count,
                ["foodNeed"] = citizensWithUnmetNeed.Sum(citizen => citizen.FoodNeed),
                ["healthNeed"] = citizensWithUnmetNeed.Sum(citizen => citizen.HealthNeed),
                ["housingNeed"] = citizensWithUnmetNeed.Sum(citizen => citizen.HousingNeed),
                ["trustPenalty"] = trustPenalty,
                ["institutionalTrustBefore"] = institutionalTrustBefore,
                ["institutionalTrustAfter"] = world.Institutions.Trust,
                ["institutionalTrustDelta"] = world.Institutions.Trust - institutionalTrustBefore,
                ["citizenTrustDelta"] = -trustPenalty
            }
        });
    }

    private static void ApplyAdministrativeEffects(WorldState world)
    {
        var capacity = Math.Max(world.Institutions.AdministrativeCapacity, world.Resources.AdminCapacity);
        if (world.Institutions.AdministrativeLoad > capacity)
        {
            var overflow = world.Institutions.AdministrativeLoad - capacity;
            var backlogBefore = world.Institutions.AppealBacklog;
            var institutionalTrustBefore = world.Institutions.Trust;
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
                    ["overflow"] = overflow,
                    ["backlogBefore"] = backlogBefore,
                    ["backlogAfter"] = world.Institutions.AppealBacklog,
                    ["backlogDelta"] = world.Institutions.AppealBacklog - backlogBefore,
                    ["institutionalTrustBefore"] = institutionalTrustBefore,
                    ["institutionalTrustAfter"] = world.Institutions.Trust,
                    ["institutionalTrustDelta"] = world.Institutions.Trust - institutionalTrustBefore
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

    private static int NeedForResource(Citizen citizen, ResourceKind resource)
    {
        return resource switch
        {
            ResourceKind.Food => citizen.FoodNeed,
            ResourceKind.Medicine => citizen.HealthNeed,
            ResourceKind.Housing => citizen.HousingNeed,
            _ => 0
        };
    }

    private static int TotalNeed(Citizen citizen)
    {
        return citizen.FoodNeed + citizen.HealthNeed + citizen.HousingNeed;
    }
}
