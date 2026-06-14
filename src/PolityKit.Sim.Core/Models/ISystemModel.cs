using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Core.Models;

public interface ISystemModel
{
    string Name { get; }

    string Version { get; }

    SystemDecision Decide(WorldState world, SystemContext context);
}
