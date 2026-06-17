using Charterfall.App.Models;

namespace Charterfall.App.Services;

public interface IPolityKitRunClient
{
    Task<PrototypeRunRecord> CreatePlaceholderRunAsync(string label, CrisisCard crisis, CancellationToken cancellationToken = default);
}
