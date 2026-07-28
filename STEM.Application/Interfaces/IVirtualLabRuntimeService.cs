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

    /// <summary>Returns false if not found. Throws UnauthorizedAccessException if owned by a different user.</summary>
    Task<bool> StopSimulationAsync(
        Guid projectId,
        int currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Throws UnauthorizedAccessException if the project is owned by a different user.</summary>
    Task MarkRunStartedAsync(
        string projectId,
        int currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Appends one simulation event atomically. Throws UnauthorizedAccessException if owned by a different user.</summary>
    Task AppendSimulationEventAsync(
        string projectId,
        object eventPayload,
        int currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Throws UnauthorizedAccessException if request.StudentId is set and differs from currentUserId.</summary>
    Task<VirtualLabSubmissionResponse> SubmitVirtualLabAsync(
        VirtualLabSubmissionRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kích hoạt compile nền (fire-and-forget, không chặn response) để làm ấm
    /// firmware cache trước khi học sinh bấm Run — dùng cho debounce lúc đang
    /// gõ code ở FE. Trả về ngay sau khi xác nhận quyền sở hữu, không đợi
    /// compile xong. Throws UnauthorizedAccessException nếu owned bởi user khác,
    /// KeyNotFoundException nếu project không tồn tại.
    /// </summary>
    Task TriggerPrecompileAsync(
        string sessionId,
        string sourceCode,
        int currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Giáo viên xem snapshot hiện tại (code/diagram/status) của 1 project
    /// học sinh — KHÔNG dùng GetDiagramAsync (chỉ owner mới gọi được) vì
    /// quyền ở đây khác hẳn: dựa trên "lab được gán cho 1 lớp giáo viên này
    /// dạy" (giống hệt VirtualLabHub.WatchStudent), không phải sở hữu
    /// project. Trả về null nếu project không tồn tại hoặc chưa gắn LabId.
    /// Throws UnauthorizedAccessException nếu giáo viên không dạy lớp nào
    /// được gán lab này.
    /// </summary>
    Task<TeacherProjectSnapshotResponse?> GetProjectSnapshotForTeacherAsync(
        string projectId,
        int teacherId,
        CancellationToken cancellationToken = default);
}
