using System.Text.Json;

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
    public string Description { get; set; } = string.Empty;
    public string AssignmentType { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public decimal MaxScore { get; set; } = 100;
    public int? RubricId { get; set; }
    public bool AllowResubmit { get; set; }
    public int? ResubmitLimit { get; set; }
    public string Status { get; set; } = "draft";
    public List<RubricCriterionRequest>? RubricCriteria { get; set; }
    public AssignmentQuizDetailRequest? QuizDetail { get; set; }
    public AssignmentReportDetailRequest? ReportDetail { get; set; }
    public AssignmentSimulationDetailRequest? SimulationDetail { get; set; }
}

public class RubricCriterionRequest
{
    public string Name { get; set; } = string.Empty;
    public int MaxPoints { get; set; }
    public string? Description { get; set; }
}

public class UpdateAssignmentRequest
{
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AssignmentType { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public decimal MaxScore { get; set; } = 100;
    public int? RubricId { get; set; }
    public bool AllowResubmit { get; set; }
    public int? ResubmitLimit { get; set; }
    public string Status { get; set; } = "draft";
    public List<RubricCriterionRequest>? RubricCriteria { get; set; }
    public AssignmentQuizDetailRequest? QuizDetail { get; set; }
    public AssignmentReportDetailRequest? ReportDetail { get; set; }
    public AssignmentSimulationDetailRequest? SimulationDetail { get; set; }
}

public class AssignmentQuizDetailRequest
{
    public JsonElement Questions { get; set; }
    public int? TimeLimitSeconds { get; set; }
    public bool ShuffleQuestions { get; set; }
}

public class AssignmentReportDetailRequest
{
    public string Instructions { get; set; } = string.Empty;
    public IReadOnlyCollection<string> AllowedSubmissionTypes { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<string> AllowedFileExtensions { get; set; } = Array.Empty<string>();
    public int MaxFileSizeMb { get; set; } = 50;
}

public class AssignmentSimulationDetailRequest
{
    public string EnvironmentSource { get; set; } = "internal_sandbox";
    public JsonElement BaseDiagram { get; set; }
    public IReadOnlyCollection<string> AllowedComponentTypes { get; set; } = Array.Empty<string>();
    public string StudentInputMode { get; set; } = "circuit_build";
    public string? StarterCode { get; set; }
    public JsonElement AnswerKey { get; set; }
    public bool AutoGradingEnabled { get; set; }
    public double AutoGradingWeight { get; set; } = 1;
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
    public string Description { get; set; } = string.Empty;
    public string AssignmentType { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public decimal MaxScore { get; set; }
    public int? RubricId { get; set; }
    public List<RubricCriterionResponse>? RubricCriteria { get; set; }
    public bool AllowResubmit { get; set; }
    public int? ResubmitLimit { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? CreatedById { get; set; }
    public AssignmentQuizDetailResponse? QuizDetail { get; set; }
    public AssignmentReportDetailResponse? ReportDetail { get; set; }
    public AssignmentSimulationDetailResponse? SimulationDetail { get; set; }
    public int SubmissionCount { get; set; }
    public int MetricCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Student submission info
    public bool HasSubmitted { get; set; }
    public decimal? HighestScore { get; set; }
    public int? LastAttemptNumber { get; set; }
    public bool CanResubmit { get; set; }
}

public class RubricCriterionResponse
{
    public string Name { get; set; } = string.Empty;
    public int MaxPoints { get; set; }
    public string? Description { get; set; }
}

public class AssignmentQuizDetailResponse
{
    public JsonElement Questions { get; set; }
    public int? TimeLimitSeconds { get; set; }
    public bool ShuffleQuestions { get; set; }
}

public class AssignmentReportDetailResponse
{
    public string Instructions { get; set; } = string.Empty;
    public JsonElement AllowedSubmissionTypes { get; set; }
    public JsonElement AllowedFileExtensions { get; set; }
    public int MaxFileSizeMb { get; set; }
}

public class AssignmentSimulationDetailResponse
{
    public string EnvironmentSource { get; set; } = string.Empty;
    public JsonElement BaseDiagram { get; set; }
    public JsonElement AllowedComponentTypes { get; set; }
    public string StudentInputMode { get; set; } = string.Empty;
    public string? StarterCode { get; set; }
    public JsonElement AnswerKey { get; set; }
    public bool AutoGradingEnabled { get; set; }
    public double AutoGradingWeight { get; set; }
}

public class SimulationValidateRequest
{
    public JsonElement Circuit { get; set; }
}

public class SimulationValidateResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PagedAssignmentResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public IReadOnlyCollection<AssignmentResponse> Items { get; set; } = Array.Empty<AssignmentResponse>();
}
