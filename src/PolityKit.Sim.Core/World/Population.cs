namespace PolityKit.Sim.Core.World;

public sealed class Population
{
    public List<Citizen> Citizens { get; init; } = [];

    public int Count => Citizens.Count;
}
