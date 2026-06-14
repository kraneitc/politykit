namespace PolityKit.Sim.Core.World;

public sealed class Citizen
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public int FoodNeed { get; set; }

    public int HealthNeed { get; set; }

    public int HousingNeed { get; set; }

    public int Wealth { get; set; }

    public int SocialPower { get; set; }

    public int TrustInSystem { get; set; }

    public int Vulnerability { get; set; }
}
