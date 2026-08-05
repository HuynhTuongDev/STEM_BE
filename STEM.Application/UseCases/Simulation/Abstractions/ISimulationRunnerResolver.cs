namespace STEM.Application.UseCases.Simulation.Abstractions;

public interface ISimulationRunnerResolver
{
    ISimulationRunner Resolve(string mode);
}
