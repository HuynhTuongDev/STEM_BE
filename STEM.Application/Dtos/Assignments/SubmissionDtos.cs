using System.Text.Json;

namespace STEM.Application.Dtos.Assignments;

public class SubmitQuizRequest
{
    public List<QuizAnswerSubmission> Answers { get; set; } = new();
}

public class QuizAnswerSubmission
{
    public string QuestionId { get; set; } = string.Empty;
    public object? Answer { get; set; }
}

public class SubmitQuizResponse
{
    public int SubmissionId { get; set; }
    public int AttemptNumber { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsAutoGraded { get; set; } = true;
    public List<QuizAnswerResult> Results { get; set; } = new();
}

public class QuizAnswerResult
{
    public string QuestionId { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public object? StudentAnswer { get; set; }
    public object? CorrectAnswer { get; set; }
}

public class SubmitReportRequest
{
    public string? Content { get; set; }
    public int? FileId { get; set; }
}

public class SubmitReportResponse
{
    public int SubmissionId { get; set; }
    public int AttemptNumber { get; set; }
    public string Status { get; set; } = "submitted";
    public DateTime SubmittedAt { get; set; }
}

public class SubmitSimulationRequest
{
    public JsonElement Circuit { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
}

public class SubmitSimulationResponse
{
    public int SubmissionId { get; set; }
    public int AttemptNumber { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public bool IsCorrect { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
    public bool IsAutoGraded { get; set; } = true;
}

public class GetMySubmissionResponse
{
    public int SubmissionId { get; set; }
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string AssignmentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public decimal? Score { get; set; }
    public decimal MaxScore { get; set; }
    public string? ContentJson { get; set; }
    public int? FileId { get; set; }
    public string? FileUrl { get; set; }
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public bool CanResubmit { get; set; }
    public int? RemainingAttempts { get; set; }
}

public class GetMySubmissionsResponse
{
    public int SubmissionId { get; set; }
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string AssignmentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public decimal? Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class PagedMySubmissionsResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public List<GetMySubmissionsResponse> Items { get; set; } = new();
}
