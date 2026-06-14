using PolityKit.Sim.Api.Services.Models;

namespace PolityKit.Sim.Api.Services;

public sealed class InMemoryRunStore : IRunStore
{
    private readonly object gate = new();
    private readonly List<StoredRun> runs = [];

    public IReadOnlyList<StoredRun> List()
    {
        lock (gate)
        {
            return runs.OrderByDescending(run => run.CreatedAt).ToArray();
        }
    }

    public StoredRun? Get(Guid id)
    {
        lock (gate)
        {
            return runs.FirstOrDefault(run => run.Id == id);
        }
    }

    public StoredRun Add(StoredRun run)
    {
        lock (gate)
        {
            runs.Add(run);
            return run;
        }
    }
}
