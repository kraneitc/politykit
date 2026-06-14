using PolityKit.Sim.Api.Services.Models;

namespace PolityKit.Sim.Api.Services;

public interface IRunStore
{
    IReadOnlyList<StoredRun> List();

    StoredRun? Get(Guid id);

    StoredRun Add(StoredRun run);
}
