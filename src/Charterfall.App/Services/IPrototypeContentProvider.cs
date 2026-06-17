using Charterfall.App.Models;

namespace Charterfall.App.Services;

public interface IPrototypeContentProvider
{
    PrototypeContent GetInitialContent();
}
