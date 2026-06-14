using Microsoft.Extensions.Options;
using PolityKit.Sim.Api.Services;
using PolityKit.Sim.Api.Services.Models;
using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Engine;

namespace PolityKit.Sim.Api.Tests.Runs;

public sealed class FileRunStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "politykit-file-run-store-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void AddPersistsRunForNewStoreInstance()
    {
        var firstStore = CreateStore();
        var run = new StoredRun
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CreatedAt = DateTimeOffset.Parse("2026-06-14T03:00:00Z"),
            Configuration = new RunConfiguration
            {
                ScenarioName = "Persistent Scenario",
                Seed = 123,
                Ticks = 5,
                ModelNames = ["NeedBasedAllocation"],
                Parameters =
                {
                    ["needPriorityWeight"] = 2.0
                }
            },
            Result = new SimulationRunResult
            {
                ScenarioName = "Persistent Scenario",
                Seed = 123,
                Ticks = 5
            }
        };

        firstStore.Add(run);
        var secondStore = CreateStore();

        var restored = secondStore.Get(run.Id);

        Assert.NotNull(restored);
        Assert.Equal(run.Id, restored.Id);
        Assert.Equal("Persistent Scenario", restored.Result.ScenarioName);
        Assert.Equal(123, restored.Result.Seed);
        Assert.Equal(5, restored.Result.Ticks);
        Assert.Equal("Persistent Scenario", restored.Configuration.ScenarioName);
        Assert.Equal(["NeedBasedAllocation"], restored.Configuration.ModelNames);
        Assert.Equal(2.0, restored.Configuration.Parameters["needPriorityWeight"]);
    }

    [Fact]
    public void ListReturnsPersistedRunsNewestFirst()
    {
        var store = CreateStore();
        var older = new StoredRun
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            CreatedAt = DateTimeOffset.Parse("2026-06-14T03:00:00Z")
        };
        var newer = new StoredRun
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            CreatedAt = DateTimeOffset.Parse("2026-06-14T04:00:00Z")
        };

        store.Add(older);
        store.Add(newer);

        var runs = CreateStore().List();

        Assert.Equal([newer.Id, older.Id], runs.Select(run => run.Id).ToArray());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private FileRunStore CreateStore()
    {
        return new FileRunStore(Options.Create(new RunStorageOptions
        {
            Directory = _directory
        }));
    }
}
