namespace PolityKit.Sim.Scenarios;

public static class ScenarioNames
{
    public static string ToSlug(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var characters = value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        return string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
