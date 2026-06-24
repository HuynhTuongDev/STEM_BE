namespace STEM.Application.Dtos.StudentLearning;

public class GetStudentAssignmentsRequest
{
    public int? ClassId { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class StudentAssignmentResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string SubmissionStatus { get; set; } = string.Empty;
    public int? SubmissionId { get; set; }
    public string? SubmissionFileUrl { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PagedStudentAssignmentResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public IReadOnlyCollection<StudentAssignmentResponse> Items { get; set; } = Array.Empty<StudentAssignmentResponse>();
}

public class SubmitAssignmentRequest
{
    public string FileUrl { get; set; } = string.Empty;
}

public class SubmissionStatusResponse
{
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public bool IsSubmitted { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? SubmissionId { get; set; }
    public int? FileId { get; set; }
    public string? FileUrl { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
}

public class GetStudentQuizzesRequest
{
    public int? CourseId { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class StudentQuizListItemResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public bool HasAttempt { get; set; }
    public decimal? LatestScore { get; set; }
    public DateTime? LatestSubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PagedStudentQuizResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public IReadOnlyCollection<StudentQuizListItemResponse> Items { get; set; } = Array.Empty<StudentQuizListItemResponse>();
}

public class StudentQuizDetailResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public IReadOnlyCollection<StudentQuizQuestionResponse> Questions { get; set; } = Array.Empty<StudentQuizQuestionResponse>();
}

public class StudentQuizQuestionResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public IReadOnlyCollection<StudentQuizAnswerOptionResponse> Answers { get; set; } = Array.Empty<StudentQuizAnswerOptionResponse>();
}

public class StudentQuizAnswerOptionResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class SubmitQuizAttemptRequest
{
    public IReadOnlyCollection<SubmitQuizAnswerRequest> Answers { get; set; } = Array.Empty<SubmitQuizAnswerRequest>();
}

public class SubmitQuizAnswerRequest
{
    public int QuestionId { get; set; }
    public int? AnswerId { get; set; }
}

public class StudentQuizResultResponse
{
    public int AttemptId { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public decimal Score { get; set; }
    public DateTime SubmittedAt { get; set; }
    public IReadOnlyCollection<StudentQuizResultAnswerResponse> Answers { get; set; } = Array.Empty<StudentQuizResultAnswerResponse>();
}

public class StudentQuizResultAnswerResponse
{
    public int QuestionId { get; set; }
    public string QuestionContent { get; set; } = string.Empty;
    public int? SelectedAnswerId { get; set; }
    public string? SelectedAnswerContent { get; set; }
    public int? CorrectAnswerId { get; set; }
    public string? CorrectAnswerContent { get; set; }
    public bool IsCorrect { get; set; }
}
