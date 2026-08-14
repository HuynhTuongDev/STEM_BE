using STEM.Core.Entities.Projects;

namespace STEM.Application.UseCases.Grading;

// Logic tính "hạn nộp thật" / "số lần nộp tối đa thật" của 1 Student cho 1
// Assignment — CỘNG DỒN các ResubmitRequest đã Approved của CHÍNH student đó
// lên trên cấu hình gốc của Assignment (Assignment.DueDate/AllowResubmit/
// ResubmitLimit không đổi — không ảnh hưởng học sinh khác). Dùng chung bởi
// VirtualLabRuntimeService.SubmitVirtualLabAsync (enforce lúc nộp) và
// CreateResubmitRequestHandler (chỉ cho tạo request khi THẬT SỰ đang bị chặn)
// — bắt buộc 2 nơi phải tính giống hệt nhau, không được lệch.
public static class ResubmitEligibility
{
    public static DateTime? GetEffectiveDueDate(
        Assignment assignment,
        IEnumerable<ResubmitRequest> approvedRequests)
    {
        DateTime? latest = assignment.DueDate;
        foreach (var request in approvedRequests)
        {
            if (request.GrantedNewDueDate.HasValue &&
                (!latest.HasValue || request.GrantedNewDueDate.Value > latest.Value))
            {
                latest = request.GrantedNewDueDate.Value;
            }
        }
        return latest;
    }

    public static int GetEffectiveMaxAttempts(
        Assignment assignment,
        IEnumerable<ResubmitRequest> approvedRequests)
    {
        var extraAttempts = approvedRequests.Sum(request => request.GrantedExtraAttempts ?? 0);

        if (!assignment.AllowResubmit)
        {
            // Base: chỉ 1 lần nộp duy nhất. Mỗi request Approved cấp thêm đúng
            // GrantedExtraAttempts lần, kể cả khi AllowResubmit=false.
            return 1 + extraAttempts;
        }

        if (!assignment.ResubmitLimit.HasValue)
        {
            return int.MaxValue; // đã không giới hạn sẵn — cộng thêm cũng vô nghĩa.
        }

        return assignment.ResubmitLimit.Value + extraAttempts;
    }

    public static bool IsBlocked(
        Assignment assignment,
        int existingSubmissionCount,
        IReadOnlyCollection<ResubmitRequest> approvedRequests,
        DateTime utcNow,
        out string? reason)
    {
        var effectiveDueDate = GetEffectiveDueDate(assignment, approvedRequests);
        if (effectiveDueDate.HasValue && utcNow > effectiveDueDate.Value)
        {
            reason = "past_due_date";
            return true;
        }

        var effectiveMaxAttempts = GetEffectiveMaxAttempts(assignment, approvedRequests);
        if (existingSubmissionCount >= effectiveMaxAttempts)
        {
            reason = "resubmit_limit_reached";
            return true;
        }

        reason = null;
        return false;
    }
}
