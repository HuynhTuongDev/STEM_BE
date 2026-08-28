using STEM.Application.Dtos.Labs;
using STEM.Application.Dtos.Simulation;

namespace STEM.Application.Interfaces;

// Lab-centric submission API — Assignment là chi tiết triển khai nội bộ
// (hidden), FE/Teacher chỉ thao tác qua LabId + ClassId, không cần biết
// AssignmentId. Xem STEM_BE/STEM.Infrastructure/Services/LabSubmissionService.cs.
public interface ILabSubmissionService
{
    /// <summary>
    /// Giáo viên xem danh sách bài nộp của 1 Lab trong 1 lớp — bắt đầu từ
    /// roster (Enrollments), LEFT JOIN Submission, không chỉ trả về những gì
    /// đã có trong bảng Submissions. Throws UnauthorizedAccessException nếu
    /// người gọi không dạy lớp đó / lab chưa gán cho lớp đó.
    /// </summary>
    Task<LabSubmissionListResponse> GetSubmissionsAsync(
        Guid labId,
        int classId,
        int currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Học sinh nộp bài cho 1 Lab — tự động resolve (hoặc tạo mới nếu chưa
    /// có) Assignment ẩn cho đúng cặp (labId, lớp của học sinh), rồi uỷ
    /// quyền cho IVirtualLabRuntimeService.SubmitVirtualLabAsync (snapshot,
    /// auto-grade, ResubmitEligibility — logic thật không đổi). Throws
    /// UnauthorizedAccessException nếu học sinh không thuộc lớp nào được
    /// gán lab này.
    /// </summary>
    Task<VirtualLabSubmissionResponse> SubmitAsync(
        Guid labId,
        SubmitLabRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default);
}
