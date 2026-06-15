namespace PolityKit.Sim.Core.Models.Governance;

public sealed record GovernanceProfile
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string Description { get; init; } = "";

    public GovernanceDimensionValue? AllocationMechanism { get; init; }

    public GovernanceDimensionValue? DecisionAuthority { get; init; }

    public GovernanceDimensionValue? AccountabilityMechanism { get; init; }

    public GovernanceDimensionValue? InformationFlow { get; init; }

    public GovernanceDimensionValue? PropertyRegime { get; init; }

    public GovernanceDimensionValue? AppealProcess { get; init; }

    public Dictionary<GovernanceDimension, Dictionary<string, double>> DimensionParameters { get; init; } = [];

    public IReadOnlyList<GovernanceDimensionValue> Dimensions()
    {
        return
        [
            .. RequiredDimensionValues().Select(item => item.Value).OfType<GovernanceDimensionValue>()
        ];
    }

    internal IReadOnlyList<(GovernanceDimension Dimension, GovernanceDimensionValue? Value)> RequiredDimensionValues()
    {
        return
        [
            (GovernanceDimension.AllocationMechanism, AllocationMechanism),
            (GovernanceDimension.DecisionAuthority, DecisionAuthority),
            (GovernanceDimension.AccountabilityMechanism, AccountabilityMechanism),
            (GovernanceDimension.InformationFlow, InformationFlow),
            (GovernanceDimension.PropertyRegime, PropertyRegime),
            (GovernanceDimension.AppealProcess, AppealProcess)
        ];
    }
}
