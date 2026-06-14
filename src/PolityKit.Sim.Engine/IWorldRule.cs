using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Engine;

public interface IWorldRule
{
    void Apply(WorldState world, SystemDecision decision);
}
