using PolityKit.Sim.Api.Services;
using PolityKit.Sim.Api.Services.Models;

namespace PolityKit.Sim.Api.Tests.Runs;

public sealed class InMemoryRunStoreTests
{
    [Fact]
    public void AddStoresAndReturnsRun()
    {
        var store = new InMemoryRunStore();
        var run = new StoredRun();

        var stored = store.Add(run);

        Assert.Same(run, stored);
        Assert.Same(run, store.Get(run.Id));
    }

    [Fact]
    public void ListReturnsRunsNewestFirst()
    {
        var store = new InMemoryRunStore();
        var older = new StoredRun
        {
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var newer = new StoredRun
        {
            CreatedAt = DateTimeOffset.UtcNow
        };

        store.Add(older);
        store.Add(newer);

        var runs = store.List();

        Assert.Equal([newer.Id, older.Id], runs.Select(run => run.Id).ToArray());
    }

    [Fact]
    public void GetReturnsNullWhenRunDoesNotExist()
    {
        var store = new InMemoryRunStore();

        var run = store.Get(Guid.NewGuid());

        Assert.Null(run);
    }
}
