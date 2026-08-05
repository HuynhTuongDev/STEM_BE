using STEM.Application.UseCases.Simulation.Runtime;

namespace STEM.Application.UseCases.Simulation.Abstractions;

public interface ISimulationRunner
{
    Task<SimulationRunResult> RunAsync(
        SimulationRunContext context,
        CancellationToken cancellationToken);
}
