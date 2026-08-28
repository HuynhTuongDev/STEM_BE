using STEM.Application.Dtos.Grading;

namespace STEM.Application.Interfaces;

// Giáo viên "Chạy mô phỏng" ngay trong màn hình chấm điểm — tạo/tái sử dụng 1
// VirtualLabProject RIÊNG (id suy ra ổn định từ SubmissionId) seed đúng
// code+diagram+board từ Submission.ContentJson, KHÔNG BAO GIỜ đụng tới
// project đang sống của học sinh. Xem GradingSessionService.cs.
public interface IGradingSessionService
{
    Task<PrepareGradingSessionResponse> PrepareAsync(
        int submissionId,
        int currentUserId,
        CancellationToken cancellationToken = default);
}
