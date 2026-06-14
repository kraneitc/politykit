using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Engine;

public sealed class DefaultShockHandler : IShockHandler
{
    public bool CanHandle(ShockDefinition shock)
    {
        return !string.IsNullOrWhiteSpace(shock.Type);
    }

    public void Apply(WorldState world, ShockDefinition shock)
    {
        var severity = Math.Clamp(shock.Severity, 0.0, 1.0);
        var data = new Dictionary<string, object>
        {
            ["shockType"] = shock.Type,
            ["severity"] = severity,
            ["requestedSeverity"] = shock.Severity
        };

        switch (shock.Type)
        {
            case "CropFailure":
                AddResourceDelta(data, ResourceKind.Food, world.Resources.Food, severity);
                data["foodProductionMultiplierBefore"] = world.Environment.FoodProductionMultiplier;
                world.Environment.FoodProductionMultiplier *= 1.0 - severity;
                world.Resources.Food = Math.Max(0, world.Resources.Food - Percent(world.Resources.Food, severity));
                data["foodProductionMultiplierAfter"] = world.Environment.FoodProductionMultiplier;
                data["foodProductionMultiplierDelta"] =
                    world.Environment.FoodProductionMultiplier - (double)data["foodProductionMultiplierBefore"];
                data["resourceAfter"] = world.Resources.Food;
                data["resourceDelta"] = world.Resources.Food - (int)data["resourceBefore"];
                break;
            case "MedicineShortage":
                AddResourceDelta(data, ResourceKind.Medicine, world.Resources.Medicine, severity);
                data["medicineSupplyMultiplierBefore"] = world.Environment.MedicineSupplyMultiplier;
                world.Environment.MedicineSupplyMultiplier *= 1.0 - severity;
                world.Resources.Medicine = Math.Max(0, world.Resources.Medicine - Percent(world.Resources.Medicine, severity));
                data["medicineSupplyMultiplierAfter"] = world.Environment.MedicineSupplyMultiplier;
                data["medicineSupplyMultiplierDelta"] =
                    world.Environment.MedicineSupplyMultiplier - (double)data["medicineSupplyMultiplierBefore"];
                data["resourceAfter"] = world.Resources.Medicine;
                data["resourceDelta"] = world.Resources.Medicine - (int)data["resourceBefore"];
                break;
            case "HousingLoss":
                AddResourceDelta(data, ResourceKind.Housing, world.Resources.Housing, severity);
                data["housingAvailabilityMultiplierBefore"] = world.Environment.HousingAvailabilityMultiplier;
                world.Environment.HousingAvailabilityMultiplier *= 1.0 - severity;
                world.Resources.Housing = Math.Max(0, world.Resources.Housing - Percent(world.Resources.Housing, severity));
                data["housingAvailabilityMultiplierAfter"] = world.Environment.HousingAvailabilityMultiplier;
                data["housingAvailabilityMultiplierDelta"] =
                    world.Environment.HousingAvailabilityMultiplier - (double)data["housingAvailabilityMultiplierBefore"];
                data["resourceAfter"] = world.Resources.Housing;
                data["resourceDelta"] = world.Resources.Housing - (int)data["resourceBefore"];
                break;
            case "AdministrativeOverload":
            case "AdminLoss":
                data["affectedResource"] = ResourceKind.AdminCapacity.ToString();
                data["adminCapacityBefore"] = world.Resources.AdminCapacity;
                data["administrativeLoadBefore"] = world.Institutions.AdministrativeLoad;
                world.Institutions.AdministrativeLoad += Percent(world.Institutions.AdministrativeCapacity, severity);
                world.Resources.AdminCapacity = Math.Max(0, world.Resources.AdminCapacity - Percent(world.Resources.AdminCapacity, severity));
                data["adminCapacityAfter"] = world.Resources.AdminCapacity;
                data["adminCapacityDelta"] = world.Resources.AdminCapacity - (int)data["adminCapacityBefore"];
                data["administrativeLoadAfter"] = world.Institutions.AdministrativeLoad;
                data["administrativeLoadDelta"] =
                    world.Institutions.AdministrativeLoad - (int)data["administrativeLoadBefore"];
                break;
            case "CorruptionSpike":
                data["corruptionBefore"] = world.Institutions.Corruption;
                data["institutionalTrustBefore"] = world.Institutions.Trust;
                world.Institutions.Corruption = Math.Clamp(world.Institutions.Corruption + severity, 0.0, 1.0);
                world.Institutions.Trust = Math.Max(0, world.Institutions.Trust - Percent(100, severity));
                data["corruptionAfter"] = world.Institutions.Corruption;
                data["corruptionDelta"] = world.Institutions.Corruption - (double)data["corruptionBefore"];
                data["institutionalTrustAfter"] = world.Institutions.Trust;
                data["institutionalTrustDelta"] =
                    world.Institutions.Trust - (int)data["institutionalTrustBefore"];
                break;
            default:
                data["stabilityBefore"] = world.Environment.Stability;
                world.Environment.Stability = Math.Clamp(world.Environment.Stability - severity * 0.1, 0.0, 1.0);
                data["stabilityAfter"] = world.Environment.Stability;
                data["stabilityDelta"] = world.Environment.Stability - (double)data["stabilityBefore"];
                break;
        }

        world.Events.Add(new SimulationEvent
        {
            Tick = world.Tick,
            Type = shock.Type,
            Description = $"Applied shock '{shock.Type}' with severity {severity:0.###}.",
            Data = data
        });
    }

    private static int Percent(int value, double percentage)
    {
        return (int)Math.Round(value * percentage, MidpointRounding.AwayFromZero);
    }

    private static void AddResourceDelta(
        Dictionary<string, object> data,
        ResourceKind resource,
        int resourceBefore,
        double severity)
    {
        data["affectedResource"] = resource.ToString();
        data["resourceBefore"] = resourceBefore;
        data["resourceReduction"] = Percent(resourceBefore, severity);
    }
}
