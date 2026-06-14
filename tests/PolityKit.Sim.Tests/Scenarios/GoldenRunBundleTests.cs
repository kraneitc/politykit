using System.Text.Json;

namespace PolityKit.Sim.Tests.Scenarios;

public sealed class GoldenRunBundleTests
{
    [Fact]
    public void GoldenInterpretedRunContainsExpectedBundleFiles()
    {
        var bundlePath = FindGoldenBundlePath();

        Assert.True(File.Exists(Path.Combine(bundlePath, "README.md")));
        Assert.True(File.Exists(Path.Combine(bundlePath, "config.json")));
        Assert.True(File.Exists(Path.Combine(bundlePath, "metrics.csv")));
        Assert.True(File.Exists(Path.Combine(bundlePath, "events.jsonl")));
        Assert.True(File.Exists(Path.Combine(bundlePath, "citizens-final.csv")));
        Assert.True(File.Exists(Path.Combine(bundlePath, "summary.json")));
    }

    [Fact]
    public void GoldenInterpretedRunSummaryShowsReadableEventLinkedBreadcrumb()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindGoldenBundlePath(), "summary.json")));
        var root = document.RootElement;

        Assert.Equal("Interpretability Demo", root.GetProperty("ScenarioName").GetString());

        var model = root.GetProperty("Models")[0];
        Assert.Equal("NeedBasedAllocation", model.GetProperty("ModelName").GetString());
        Assert.Equal(1, model.GetProperty("EventCountsByType").GetProperty("CorruptionSpike").GetInt32());

        var change = model.GetProperty("NotableMetricChanges")[0];
        Assert.Equal("Trust", change.GetProperty("Metric").GetString());
        Assert.Equal("Trust dropped after CorruptionSpike at tick 6.", change.GetProperty("Breadcrumb").GetString());

        var nearbyEvents = change.GetProperty("NearbyEvents").EnumerateArray().ToArray();
        Assert.Contains(nearbyEvents, nearbyEvent =>
            nearbyEvent.GetProperty("Type").GetString() == "CorruptionSpike"
            && nearbyEvent.GetProperty("Data").GetProperty("institutionalTrustDelta").GetInt32() == -25);
    }

    private static string FindGoldenBundlePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var bundlePath = Path.Combine(directory.FullName, "examples", "golden-interpreted-run");
            if (Directory.Exists(bundlePath))
            {
                return bundlePath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find examples/golden-interpreted-run.");
    }
}
