namespace PolityKit.Sim.Engine;

public interface ISimulationEngine
{
    SimulationRunResult Run(SimulationRunRequest request);
}
