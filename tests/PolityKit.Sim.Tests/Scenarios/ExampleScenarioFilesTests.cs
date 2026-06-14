using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Tests.Scenarios;

public sealed class ExampleScenarioFilesTests
{
    [Fact]
    public void AllExampleScenarioFilesLoadAndValidate()
    {
        var examplesPath = FindExamplesPath();
        var scenarioFiles = Directory.GetFiles(examplesPath, "*.json");
        var loader = new JsonScenarioLoader();

        Assert.NotEmpty(scenarioFiles);

        foreach (var scenarioFile in scenarioFiles)
        {
            var scenario = loader.Load(scenarioFile);

            Assert.False(string.IsNullOrWhiteSpace(scenario.Name));
            Assert.True(scenario.Ticks > 0);
        }
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
