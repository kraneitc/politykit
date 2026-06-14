using System.Text.Json;
using PolityKit.Sim.Core.Scenarios;

namespace PolityKit.Sim.Scenarios;

public sealed class JsonScenarioLoader(IScenarioValidator? validator = null) : IScenarioLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly IScenarioValidator _validator = validator ?? new ScenarioValidator();

    public ScenarioDefinition Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Scenario file '{path}' was not found.", path);
        }

        var scenario = JsonSerializer.Deserialize<ScenarioDefinition>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException($"Scenario file '{path}' could not be read.");

        var validation = _validator.Validate(scenario);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Scenario file '{path}' is invalid: {string.Join("; ", validation.Errors)}");
        }

        return scenario;
    }
}
