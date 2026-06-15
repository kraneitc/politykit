namespace PolityKit.Sim.Core.Models.Governance;

public sealed record GovernanceDimensionValue(
    GovernanceDimension Dimension,
    string Id,
    string DisplayName,
    string Description = "");
