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

        switch (shock.Type)
        {
            case "CropFailure":
                world.Environment.FoodProductionMultiplier *= 1.0 - severity;
                world.Resources.Food = Math.Max(0, world.Resources.Food - Percent(world.Resources.Food, severity));
                break;
            case "MedicineShortage":
                world.Environment.MedicineSupplyMultiplier *= 1.0 - severity;
                world.Resources.Medicine = Math.Max(0, world.Resources.Medicine - Percent(world.Resources.Medicine, severity));
                break;
            case "HousingLoss":
                world.Environment.HousingAvailabilityMultiplier *= 1.0 - severity;
                world.Resources.Housing = Math.Max(0, world.Resources.Housing - Percent(world.Resources.Housing, severity));
                break;
            case "AdministrativeOverload":
            case "AdminLoss":
                world.Institutions.AdministrativeLoad += Percent(world.Institutions.AdministrativeCapacity, severity);
                world.Resources.AdminCapacity = Math.Max(0, world.Resources.AdminCapacity - Percent(world.Resources.AdminCapacity, severity));
                break;
            case "CorruptionSpike":
                world.Institutions.Corruption = Math.Clamp(world.Institutions.Corruption + severity, 0.0, 1.0);
                world.Institutions.Trust = Math.Max(0, world.Institutions.Trust - Percent(100, severity));
                break;
            default:
                world.Environment.Stability = Math.Clamp(world.Environment.Stability - severity * 0.1, 0.0, 1.0);
                break;
        }

        world.Events.Add(new SimulationEvent
        {
            Tick = world.Tick,
            Type = shock.Type,
            Description = $"Applied shock '{shock.Type}' with severity {severity:0.###}.",
            Data =
            {
                ["severity"] = severity
            }
        });
    }

    private static int Percent(int value, double percentage)
    {
        return (int)Math.Round(value * percentage, MidpointRounding.AwayFromZero);
    }
}
