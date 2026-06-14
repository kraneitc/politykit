namespace PolityKit.Sim.Core.Simulation;

public interface IRandomSource
{
    int Seed { get; }

    int Next(int minValue, int maxValue);
}
