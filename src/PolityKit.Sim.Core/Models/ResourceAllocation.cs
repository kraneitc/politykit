using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Core.Models;

public sealed class ResourceAllocation
{
    public Guid? CitizenId { get; init; }

    public ResourceKind Resource { get; init; }

    public int Amount { get; init; }

    public double Priority { get; init; }

    public string Reason { get; init; } = "";
}
