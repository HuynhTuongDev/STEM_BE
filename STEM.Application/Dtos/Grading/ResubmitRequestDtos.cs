namespace STEM.Application.Dtos.Grading;

public class CreateResubmitRequestRequest
{
    public int AssignmentId { get; set; }
    public string? Reason { get; set; }
}

public class ReviewResubmitRequestRequest
{
    public int? ExtraAttempts { get; set; }
    public DateTime? NewDueDate { get; set; }
    public string? Note { get; set; }
}

// Chiều NGƯỢC với CreateResubmitRequestRequest: giáo viên chủ động yêu cầu 1
// học sinh nộp lại (vd bài nộp có lỗi rõ ràng), thay vì học sinh xin phép khi
// đã hết lượt nộp. Tạo thẳng 1 ResubmitRequest ở trạng thái Approved (không
// qua Pending — chính giáo viên là người "duyệt" ngay lúc yêu cầu), dùng
// đúng cơ chế GrantedExtraAttempts đã có sẵn (xem ResubmitEligibility) để
// cấp thêm lượt nộp, kể cả khi Assignment.AllowResubmit=false.
public class TeacherRequestResubmitRequest
{
    public string? Note { get; set; }
    public int? ExtraAttempts { get; set; }
}

public class GetResubmitRequestsQuery
{
    public int? AssignmentId { get; set; }
    public string? Status { get; set; }
}

public class ResubmitRequestResponse
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? GrantedExtraAttempts { get; set; }
    public DateTime? GrantedNewDueDate { get; set; }
    public string? ReviewNote { get; set; }
    public int? ReviewedById { get; set; }
    public string ReviewedByName { get; set; } = string.Empty;
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
