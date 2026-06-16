using System.Text.Json;

namespace PolityKit.Sim.Tests.Scenarios;

public sealed class ScenarioSchemaTests
{
    [Fact]
    public void ScenarioSchemaIsValidJsonAndDocumentsRequiredFields()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(FindSchemaPath()));
        var root = schema.RootElement;

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
        Assert.Equal("PolityKit Scenario", root.GetProperty("title").GetString());

        var requiredFields = root.GetProperty("required")
            .EnumerateArray()
            .Select(field => field.GetString()!)
            .ToArray();
        Assert.Equal(["name", "ticks", "initialPopulation", "initialResources"], requiredFields);

        var resourcesRequired = root
            .GetProperty("$defs")
            .GetProperty("resourcePool")
            .GetProperty("required")
            .EnumerateArray()
            .Select(field => field.GetString()!)
            .ToArray();
        Assert.Equal(["food", "medicine", "housing", "adminCapacity", "productionCapacity"], resourcesRequired);

        var shockRequired = root
            .GetProperty("$defs")
            .GetProperty("shock")
            .GetProperty("required")
            .EnumerateArray()
            .Select(field => field.GetString()!)
            .ToArray();
        Assert.Equal(["tick", "type", "severity"], shockRequired);
    }

    [Fact]
    public void ExampleScenarioFilesSatisfyScenarioSchemaConstraints()
    {
        var examplesPath = FindExamplesPath();
        var scenarioFiles = Directory.GetFiles(examplesPath, "*.json");

        Assert.NotEmpty(scenarioFiles);

        foreach (var scenarioFile in scenarioFiles)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(scenarioFile));
            var root = document.RootElement;

            AssertNonBlankString(root, "name", scenarioFile);
            AssertNonNegativeInteger(root, "seed", scenarioFile, required: false);
            var ticks = AssertPositiveInteger(root, "ticks", scenarioFile);
            AssertNonNegativeInteger(root, "initialPopulation", scenarioFile, required: true);

            var resources = root.GetProperty("initialResources");
            AssertNonNegativeInteger(resources, "food", scenarioFile, required: true);
            AssertNonNegativeInteger(resources, "medicine", scenarioFile, required: true);
            AssertNonNegativeInteger(resources, "housing", scenarioFile, required: true);
            AssertNonNegativeInteger(resources, "adminCapacity", scenarioFile, required: true);
            AssertNonNegativeInteger(resources, "productionCapacity", scenarioFile, required: true);

            if (!root.TryGetProperty("shocks", out var shocks))
            {
                continue;
            }

            Assert.Equal(JsonValueKind.Array, shocks.ValueKind);
            foreach (var shock in shocks.EnumerateArray())
            {
                var tick = AssertNonNegativeInteger(shock, "tick", scenarioFile, required: true);
                Assert.True(tick < ticks, $"Shock tick {tick} in {scenarioFile} must be less than scenario ticks {ticks}.");
                AssertNonBlankString(shock, "type", scenarioFile);
                var severity = AssertNumber(shock, "severity", scenarioFile);
                Assert.InRange(severity, 0, 1);
            }
        }
    }

    private static void AssertNonBlankString(JsonElement element, string propertyName, string source)
    {
        Assert.True(element.TryGetProperty(propertyName, out var value), $"{source} must include '{propertyName}'.");
        Assert.Equal(JsonValueKind.String, value.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(value.GetString()), $"{source} property '{propertyName}' must not be blank.");
    }

    private static int AssertPositiveInteger(JsonElement element, string propertyName, string source)
    {
        var value = AssertNonNegativeInteger(element, propertyName, source, required: true);
        Assert.True(value > 0, $"{source} property '{propertyName}' must be greater than zero.");
        return value;
    }

    private static int AssertNonNegativeInteger(
        JsonElement element,
        string propertyName,
        string source,
        bool required)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            Assert.False(required, $"{source} must include '{propertyName}'.");
            return 0;
        }

        Assert.Equal(JsonValueKind.Number, value.ValueKind);
        Assert.True(value.TryGetInt32(out var integer), $"{source} property '{propertyName}' must be an integer.");
        Assert.True(integer >= 0, $"{source} property '{propertyName}' cannot be negative.");
        return integer;
    }

    private static double AssertNumber(JsonElement element, string propertyName, string source)
    {
        Assert.True(element.TryGetProperty(propertyName, out var value), $"{source} must include '{propertyName}'.");
        Assert.Equal(JsonValueKind.Number, value.ValueKind);
        return value.GetDouble();
    }

    private static string FindSchemaPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var schemaPath = Path.Combine(directory.FullName, "docs", "politykit", "scenario.schema.json");
            if (File.Exists(schemaPath))
            {
                return schemaPath;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not find docs/politykit/scenario.schema.json.");
    }

    private static string FindExamplesPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var examplesPath = Path.Combine(directory.FullName, "examples");
            if (Directory.Exists(examplesPath))
            {
                return examplesPath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the examples directory.");
    }
}
