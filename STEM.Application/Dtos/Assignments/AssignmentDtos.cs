namespace STEM.Application.Dtos.Assignments;

public class GetAssignmentsRequest
{
    public string? SearchTerm { get; set; }
    public int? ClassId { get; set; }
    public int? CourseId { get; set; }
    public int? StudentId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateAssignmentRequest
{
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class UpdateAssignmentRequest
{
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class AssignmentResponse
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SubmissionCount { get; set; }
    public int MetricCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PagedAssignmentResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public IReadOnlyCollection<AssignmentResponse> Items { get; set; } = Array.Empty<AssignmentResponse>();
}
