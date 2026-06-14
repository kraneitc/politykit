using PolityKit.Sim.Core.Models;

namespace PolityKit.Sim.Models;

public interface IModelCatalog
{
    IReadOnlyList<ISystemModel> All { get; }

    ISystemModel? FindByName(string name);
}
