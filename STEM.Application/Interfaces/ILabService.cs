using STEM.Application.Dtos.Labs;

namespace STEM.Application.Interfaces;

public interface ILabService
{
    Task<PagedLabResponse> GetLabsAsync(
        GetLabsRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<LabResponse> GetLabAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<LabResponse> CreateLabAsync(
        CreateLabRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<LabResponse> UpdateLabAsync(
        Guid id,
        UpdateLabRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task DeleteLabAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<ValidateWokwiProjectResponse> ValidateWokwiProjectAsync(
        ValidateWokwiProjectRequest request,
        CancellationToken cancellationToken = default);

    Task<ValidateWokwiProjectResponse> ValidateExistingWokwiProjectAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<LabProgressResponse> StartProgressAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<LabProgressResponse> CompleteProgressAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    // Đọc thuần (không mutate) — khác StartProgressAsync (side-effect: tạo/OpenCount++).
    // Dùng để render danh sách nhiều lab cùng lúc (vd. tab Phòng Lab Ảo trong Class
    // Detail) mà không âm thầm "start" progress cho lab học sinh chưa từng mở.
    Task<IReadOnlyCollection<LabProgressResponse>> GetMyProgressAsync(
        int? classId,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<LabStatsResponse> GetStatsAsync(
        Guid id,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ComponentGlueRegistryResponse>> GetComponentGlueRegistryAsync(
        bool supportedOnly = true,
        CancellationToken cancellationToken = default);
}
