using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Projects;

// Student hết ResubmitLimit hoặc quá DueDate -> tạo request xin nộp thêm cho
// đúng 1 Assignment. Teacher approve sẽ cấp thêm quyền CHỈ cho student đó
// (không đổi Assignment.ResubmitLimit/DueDate chung — không ảnh hưởng học
// sinh khác) qua GrantedExtraAttempts và/hoặc GrantedNewDueDate — xem cách
// 2 field này được cộng dồn vào effective limit/deadline lúc submit trong
// VirtualLabRuntimeService.SubmitVirtualLabAsync.
public class ResubmitRequest : BaseEntity
{
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = ResubmitRequestStatuses.Pending;

    public int? GrantedExtraAttempts { get; set; }
    public DateTime? GrantedNewDueDate { get; set; }
    public string? ReviewNote { get; set; }
    public int? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Assignment? Assignment { get; set; }
    public User? Student { get; set; }
    public User? ReviewedBy { get; set; }
}
