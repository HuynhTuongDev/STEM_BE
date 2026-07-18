using STEM.Application.Dtos.Simulation;

namespace STEM.Application.Interfaces;

public interface IVirtualLabRuntimeService
{
    /// <summary>Returns null if not found. Throws UnauthorizedAccessException if the project exists but is owned by a different user.</summary>
    Task<DiagramSessionResponse?> GetDiagramAsync(
        string sessionId,
        int currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Throws UnauthorizedAccessException if the project exists but is owned by a different user.</summary>
    Task<DiagramSessionResponse> SaveDiagramAsync(
        string sessionId,
        SaveDiagramRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<RunEsp32SimulationResponse> RunEsp32Async(
        RunEsp32SimulationRequest request,
        int? currentUserId,
        CancellationToken cancellationToken = default);

    Task<VirtualLabSubmissionResponse> SubmitVirtualLabAsync(
        VirtualLabSubmissionRequest request,
        int? currentUserId,
        CancellationToken cancellationToken = default);
}
