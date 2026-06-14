using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Engine;

public interface IShockHandler
{
    bool CanHandle(ShockDefinition shock);

    void Apply(WorldState world, ShockDefinition shock);
}
