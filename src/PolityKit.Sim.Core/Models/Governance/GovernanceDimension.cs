namespace PolityKit.Sim.Core.Models.Governance;

public enum GovernanceDimension
{
    AllocationMechanism,
    DecisionAuthority,
    AccountabilityMechanism,
    InformationFlow,
    PropertyRegime,
    AppealProcess
}

public static class GovernanceDimensionExtensions
{
    public static string GetId(this GovernanceDimension dimension)
    {
        return dimension switch
        {
            GovernanceDimension.AllocationMechanism => "allocation-mechanism",
            GovernanceDimension.DecisionAuthority => "decision-authority",
            GovernanceDimension.AccountabilityMechanism => "accountability-mechanism",
            GovernanceDimension.InformationFlow => "information-flow",
            GovernanceDimension.PropertyRegime => "property-regime",
            GovernanceDimension.AppealProcess => "appeal-process",
            _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null)
        };
    }

    public static string GetDisplayName(this GovernanceDimension dimension)
    {
        return dimension switch
        {
            GovernanceDimension.AllocationMechanism => "Allocation Mechanism",
            GovernanceDimension.DecisionAuthority => "Decision Authority",
            GovernanceDimension.AccountabilityMechanism => "Accountability Mechanism",
            GovernanceDimension.InformationFlow => "Information Flow",
            GovernanceDimension.PropertyRegime => "Property Regime",
            GovernanceDimension.AppealProcess => "Appeal Process",
            _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null)
        };
    }
}
