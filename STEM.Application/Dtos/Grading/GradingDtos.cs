using STEM.Application.Dtos.Assignments;

namespace STEM.Application.Dtos.Grading;

public class GetSubmissionsRequest
{
    public int? AssignmentId { get; set; }
    public int? ClassId { get; set; }
    public int? StudentId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GradeSubmissionRequest
{
    public decimal Score { get; set; }
    public string? Feedback { get; set; }
}

public class SubmissionResponse
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int? StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int? FileId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ContentJson { get; set; } = "{}";
    public string? AutoGradeResultJson { get; set; }
    public decimal? AutoScore { get; set; }
    public decimal? FinalScore { get; set; }
    public int AttemptNumber { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
    public int? GradedById { get; set; }
    public string GradedByName { get; set; } = string.Empty;
    public DateTime? GradedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Assignment details
    public decimal MaxScore { get; set; }
    public string AssignmentType { get; set; } = string.Empty;
    public List<RubricCriterionResponse>? RubricCriteria { get; set; }
}

public class PagedSubmissionResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public IReadOnlyCollection<SubmissionResponse> Items { get; set; } = Array.Empty<SubmissionResponse>();
}
