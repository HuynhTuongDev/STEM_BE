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
