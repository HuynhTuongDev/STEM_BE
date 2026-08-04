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
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public string AssignmentType { get; set; } = "File";
    public decimal MaxScore { get; set; } = 100;
    public DateTime? DueDate { get; set; }
    public int? LinkedLabId { get; set; }
    public int? LinkedQuizId { get; set; }
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
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public string AssignmentType { get; set; } = "File";
    public decimal MaxScore { get; set; } = 100;
    public DateTime? DueDate { get; set; }
    public int? LinkedLabId { get; set; }
    public int? LinkedQuizId { get; set; }
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

public class AssignmentDetailResponse : AssignmentResponse
{
    public List<SubmissionResponse> Submissions { get; set; } = new();
    public List<MetricResponse> Metrics { get; set; } = new();
}

public class SubmissionResponse
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int? StudentId { get; set; }
    public string? StudentName { get; set; }
    public int FileId { get; set; }
    public string? FileName { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
    public decimal? AutoScore { get; set; }
    public decimal? FinalScore { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? GradedAt { get; set; }
    public string? GradedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MetricResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal MaxScore { get; set; }
}

public class AssignmentQuizDetail
{
    public int AssignmentId { get; set; }
    public int QuizId { get; set; }
    public string? QuizTitle { get; set; }
    public int QuestionCount { get; set; }
    public int TimeLimit { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShowCorrectAnswers { get; set; }
}

public class AssignmentSimulationDetail
{
    public int AssignmentId { get; set; }
    public int LabId { get; set; }
    public string? LabTitle { get; set; }
    public string SimulationMode { get; set; } = string.Empty;
    public string BoardType { get; set; } = string.Empty;
    public int MaxAttempts { get; set; }
    public bool AllowCodeSubmission { get; set; }
}

public class AssignmentReportDetail
{
    public int AssignmentId { get; set; }
    public string ReportTemplate { get; set; } = string.Empty;
    public bool RequireCitations { get; set; }
    public int MinWordCount { get; set; }
    public string? RubricId { get; set; }
}
