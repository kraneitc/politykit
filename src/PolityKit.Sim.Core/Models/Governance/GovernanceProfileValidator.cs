namespace PolityKit.Sim.Core.Models.Governance;

public static class GovernanceProfileValidator
{
    public static GovernanceProfileValidationResult Validate(GovernanceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            errors.Add("Governance profile id is required.");
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            errors.Add("Governance profile name is required.");
        }

        ValidateDimensions(profile, errors);
        ValidateParameters(profile, errors);

        return new GovernanceProfileValidationResult
        {
            Errors = errors
        };
    }

    private static void ValidateDimensions(GovernanceProfile profile, List<string> errors)
    {
        foreach (var (dimension, value) in profile.RequiredDimensionValues())
        {
            var dimensionName = dimension.GetDisplayName();
            if (value is null)
            {
                errors.Add($"{dimensionName} is required.");
                continue;
            }

            if (value.Dimension != dimension)
            {
                errors.Add($"{dimensionName} value must use dimension id '{dimension.GetId()}'.");
            }

            if (string.IsNullOrWhiteSpace(value.Id))
            {
                errors.Add($"{dimensionName} value id is required.");
            }

            if (string.IsNullOrWhiteSpace(value.DisplayName))
            {
                errors.Add($"{dimensionName} value display name is required.");
            }
        }
    }

    private static void ValidateParameters(GovernanceProfile profile, List<string> errors)
    {
        foreach (var (dimension, parameters) in profile.DimensionParameters)
        {
            if (!Enum.IsDefined(dimension))
            {
                errors.Add($"Governance dimension '{dimension}' is not supported.");
                continue;
            }

            if (parameters is null)
            {
                errors.Add($"{dimension.GetDisplayName()} parameters cannot be null.");
                continue;
            }

            foreach (var (name, value) in parameters)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add($"{dimension.GetDisplayName()} parameter names cannot be blank.");
                }

                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    errors.Add($"{dimension.GetDisplayName()} parameter '{name}' must be a finite number.");
                }
            }
        }
    }
}
