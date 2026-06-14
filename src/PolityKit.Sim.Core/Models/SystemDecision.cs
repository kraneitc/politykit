namespace PolityKit.Sim.Core.Models;

public sealed class SystemDecision
{
    public List<ResourceAllocation> Allocations { get; init; } = [];

    public List<PolicyChange> PolicyChanges { get; init; } = [];

    public List<InstitutionalAction> InstitutionalActions { get; init; } = [];
}
