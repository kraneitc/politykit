using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.Simulation;
using PolityKit.Sim.Core.World;

namespace PolityKit.Sim.Engine;

public interface IWorldFactory
{
    WorldState CreateWorld(ScenarioDefinition scenario, IRandomSource random);
}
