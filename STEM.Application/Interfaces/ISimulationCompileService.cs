using STEM.Application.Dtos.Simulation;

namespace STEM.Application.Interfaces;

public interface ISimulationCompileService
{
    Task<CompileSimulationResponse> CompileAsync(
        CompileSimulationRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<CompileJobResponse?> GetJobAsync(
        string jobId,
        int currentUserId,
        CancellationToken cancellationToken = default);
}
