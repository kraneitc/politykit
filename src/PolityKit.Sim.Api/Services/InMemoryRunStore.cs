using PolityKit.Sim.Api.Services.Models;

namespace PolityKit.Sim.Api.Services;

public sealed class InMemoryRunStore : IRunStore
{
    private readonly Lock _gate = new();
    private readonly List<StoredRun> _runs = [];

    public IReadOnlyList<StoredRun> List()
    {
        lock (_gate)
        {
            return _runs.OrderByDescending(run => run.CreatedAt).ToArray();
        }
    }

    public StoredRun? Get(Guid id)
    {
        lock (_gate)
        {
            return _runs.FirstOrDefault(run => run.Id == id);
        }
    }

    public StoredRun Add(StoredRun run)
    {
        lock (_gate)
        {
            _runs.Add(run);
            return run;
        }
    }
}
