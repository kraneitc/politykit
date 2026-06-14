namespace PolityKit.Sim.Core.Models;

public sealed class InstitutionalAction
{
    public string Type { get; init; } = "";

    public string Description { get; init; } = "";

    public int AdministrativeCost { get; init; }
}
