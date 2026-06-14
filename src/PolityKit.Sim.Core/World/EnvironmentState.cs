namespace PolityKit.Sim.Core.World;

public sealed class EnvironmentState
{
    public double FoodProductionMultiplier { get; set; } = 1.0;

    public double MedicineSupplyMultiplier { get; set; } = 1.0;

    public double HousingAvailabilityMultiplier { get; set; } = 1.0;

    public double Stability { get; set; } = 1.0;
}
