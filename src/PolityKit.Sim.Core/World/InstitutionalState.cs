namespace PolityKit.Sim.Core.World;

public sealed class InstitutionalState
{
    public int AdministrativeCapacity { get; set; }

    public int AdministrativeLoad { get; set; }

    public int AppealBacklog { get; set; }

    public int Trust { get; set; }

    public double Corruption { get; set; }

    public double Legitimacy { get; set; } = 1.0;
}
