using PolityKit.Sim.Core.Models.Governance;

namespace PolityKit.Sim.Tests.Core;

public sealed class GovernanceProfileTests
{
    [Fact]
    public void DimensionMetadataHasStableIdsAndDisplayNames()
    {
        Assert.Equal("allocation-mechanism", GovernanceDimension.AllocationMechanism.GetId());
        Assert.Equal("Allocation Mechanism", GovernanceDimension.AllocationMechanism.GetDisplayName());
        Assert.Equal("decision-authority", GovernanceDimension.DecisionAuthority.GetId());
        Assert.Equal("Decision Authority", GovernanceDimension.DecisionAuthority.GetDisplayName());
        Assert.Equal("accountability-mechanism", GovernanceDimension.AccountabilityMechanism.GetId());
        Assert.Equal("Accountability Mechanism", GovernanceDimension.AccountabilityMechanism.GetDisplayName());
        Assert.Equal("information-flow", GovernanceDimension.InformationFlow.GetId());
        Assert.Equal("Information Flow", GovernanceDimension.InformationFlow.GetDisplayName());
        Assert.Equal("property-regime", GovernanceDimension.PropertyRegime.GetId());
        Assert.Equal("Property Regime", GovernanceDimension.PropertyRegime.GetDisplayName());
        Assert.Equal("appeal-process", GovernanceDimension.AppealProcess.GetId());
        Assert.Equal("Appeal Process", GovernanceDimension.AppealProcess.GetDisplayName());
    }

    [Fact]
    public void CompleteProfileValidates()
    {
        var profile = CreateValidProfile();

        var result = GovernanceProfileValidator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void DimensionsReturnsConfiguredDimensionValues()
    {
        var profile = CreateValidProfile();

        var dimensions = profile.Dimensions();

        Assert.Equal(6, dimensions.Count);
        Assert.Equal(
            [
                GovernanceDimension.AllocationMechanism,
                GovernanceDimension.DecisionAuthority,
                GovernanceDimension.AccountabilityMechanism,
                GovernanceDimension.InformationFlow,
                GovernanceDimension.PropertyRegime,
                GovernanceDimension.AppealProcess
            ],
            dimensions.Select(dimension => dimension.Dimension).ToArray());
    }

    [Fact]
    public void ValidationReportsMissingRequiredProfileAndDimensionFields()
    {
        var result = GovernanceProfileValidator.Validate(new GovernanceProfile());

        Assert.False(result.IsValid);
        Assert.Contains("Governance profile id is required.", result.Errors);
        Assert.Contains("Governance profile name is required.", result.Errors);
        Assert.Contains("Allocation Mechanism is required.", result.Errors);
        Assert.Contains("Decision Authority is required.", result.Errors);
        Assert.Contains("Accountability Mechanism is required.", result.Errors);
        Assert.Contains("Information Flow is required.", result.Errors);
        Assert.Contains("Property Regime is required.", result.Errors);
        Assert.Contains("Appeal Process is required.", result.Errors);
    }

    [Fact]
    public void ValidationReportsMismatchedDimensionValue()
    {
        var profile = CreateValidProfile() with
        {
            AllocationMechanism = Value(GovernanceDimension.DecisionAuthority, "lottery", "Lottery")
        };

        var result = GovernanceProfileValidator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Allocation Mechanism value must use dimension id 'allocation-mechanism'.", result.Errors);
    }

    [Fact]
    public void ValidationReportsBlankDimensionValueMetadata()
    {
        var profile = CreateValidProfile() with
        {
            AppealProcess = Value(GovernanceDimension.AppealProcess, "", "")
        };

        var result = GovernanceProfileValidator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Appeal Process value id is required.", result.Errors);
        Assert.Contains("Appeal Process value display name is required.", result.Errors);
    }

    [Fact]
    public void ValidationReportsInvalidDimensionParameters()
    {
        var profile = CreateValidProfile();
        profile.DimensionParameters[GovernanceDimension.AllocationMechanism][""] = 1;
        profile.DimensionParameters[GovernanceDimension.DecisionAuthority] = new Dictionary<string, double>
        {
            ["decisionNoise"] = double.NaN
        };
        profile.DimensionParameters[(GovernanceDimension)999] = new Dictionary<string, double>
        {
            ["weight"] = 1
        };

        var result = GovernanceProfileValidator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains("Allocation Mechanism parameter names cannot be blank.", result.Errors);
        Assert.Contains("Decision Authority parameter 'decisionNoise' must be a finite number.", result.Errors);
        Assert.Contains("Governance dimension '999' is not supported.", result.Errors);
    }

    [Fact]
    public void ValidateRejectsNullProfile()
    {
        Assert.Throws<ArgumentNullException>(() => GovernanceProfileValidator.Validate(null!));
    }

    private static GovernanceProfile CreateValidProfile()
    {
        return new GovernanceProfile
        {
            Id = "test-profile",
            Name = "Test Profile",
            Description = "A test governance profile.",
            AllocationMechanism = Value(GovernanceDimension.AllocationMechanism, "need-weighted", "Need Weighted"),
            DecisionAuthority = Value(GovernanceDimension.DecisionAuthority, "council", "Council"),
            AccountabilityMechanism = Value(GovernanceDimension.AccountabilityMechanism, "audit", "Audit"),
            InformationFlow = Value(GovernanceDimension.InformationFlow, "transparent", "Transparent"),
            PropertyRegime = Value(GovernanceDimension.PropertyRegime, "mixed", "Mixed"),
            AppealProcess = Value(GovernanceDimension.AppealProcess, "formal-review", "Formal Review"),
            DimensionParameters =
            {
                [GovernanceDimension.AllocationMechanism] = new Dictionary<string, double>
                {
                    ["needWeight"] = 1.0
                },
                [GovernanceDimension.AccountabilityMechanism] = new Dictionary<string, double>
                {
                    ["auditFrequency"] = 0.5
                }
            }
        };
    }

    private static GovernanceDimensionValue Value(GovernanceDimension dimension, string id, string displayName)
    {
        return new GovernanceDimensionValue(dimension, id, displayName);
    }
}
