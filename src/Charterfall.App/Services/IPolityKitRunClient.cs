using Charterfall.App.Models;

namespace Charterfall.App.Services;

public interface IPolityKitRunClient
{
    Task<CreateRunResult> CreateRunAsync(CreateRunInput input, CancellationToken cancellationToken = default);
}
