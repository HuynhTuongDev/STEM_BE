namespace STEM.Application.Dtos.Labs;

// Teacher-facing DTO — không expose AssignmentId ra ngoài. Assignment là chi
// tiết triển khai nội bộ (hidden) để thoả yêu cầu FK bắt buộc của
// Submission.AssignmentId, giáo viên không cần biết tới nó.
public class LabSubmissionListResponse
{
    public string LabId { get; set; } = string.Empty;
    public string LabTitle { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int SubmittedCount { get; set; }
    public int NotSubmittedCount { get; set; }
    public IReadOnlyCollection<LabSubmissionStudentRow> Students { get; set; } = Array.Empty<LabSubmissionStudentRow>();
}

public class LabSubmissionStudentRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;

    // "not_started" | "in_progress" | "submitted" | "graded" — suy ra từ
    // LabProgress (chưa nộp) hoặc Submission (đã nộp/đã chấm), không phải
    // giá trị cột DB nào trực tiếp.
    public string Status { get; set; } = string.Empty;

    public int? SubmissionId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? Score { get; set; }
    public int? AttemptNumber { get; set; }
}

public class SubmitLabRequest
{
    public string? SessionId { get; set; }
    public string DiagramJson { get; set; } = "{}";
    public string SourceCode { get; set; } = string.Empty;
}
