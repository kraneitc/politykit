using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Tests.Scenarios;

public sealed class JsonScenarioLoaderTests
{
    [Fact]
    public void LoadReadsScenarioFromJsonCaseInsensitively()
    {
        var path = WriteTempScenarioJson("""
            {
              "name": "Loaded Scenario",
              "seed": 123,
              "ticks": 5,
              "initialPopulation": 2,
              "initialResources": {
                "food": 10,
                "medicine": 20,
                "housing": 30,
                "adminCapacity": 40,
                "productionCapacity": 50
              },
              "shocks": [
                {
                  "tick": 2,
                  "type": "CropFailure",
                  "severity": 0.25,
                  "parameters": {
                    "note": "test"
                  }
                }
              ]
            }
            """);
        var loader = new JsonScenarioLoader();

        var scenario = loader.Load(path);

        Assert.Equal("Loaded Scenario", scenario.Name);
        Assert.Equal(123, scenario.Seed);
        Assert.Equal(5, scenario.Ticks);
        Assert.Equal(2, scenario.InitialPopulation);
        Assert.Equal(10, scenario.InitialResources.Food);
        Assert.Single(scenario.Shocks);
        Assert.Equal("CropFailure", scenario.Shocks[0].Type);
    }

    [Fact]
    public void LoadRejectsInvalidScenario()
    {
        var path = WriteTempScenarioJson("""
            {
              "name": "",
              "ticks": 0
            }
            """);
        var loader = new JsonScenarioLoader();

        var exception = Assert.Throws<InvalidOperationException>(() => loader.Load(path));

        Assert.Contains("is invalid", exception.Message);
        Assert.Contains("Scenario name is required.", exception.Message);
    }

    [Fact]
    public void LoadRejectsMissingFileAndBlankPath()
    {
        var loader = new JsonScenarioLoader();

        Assert.Throws<ArgumentException>(() => loader.Load(""));
        Assert.Throws<FileNotFoundException>(() => loader.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")));
    }

    private static string WriteTempScenarioJson(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"politykit-scenario-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
