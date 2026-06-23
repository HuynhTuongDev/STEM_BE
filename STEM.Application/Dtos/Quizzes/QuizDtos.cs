namespace STEM.Application.Dtos.Quizzes;

public class GetQuizzesRequest
{
    public string? SearchTerm { get; set; }
    public int? CourseId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateQuizRequest
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<CreateQuizQuestionRequest> Questions { get; set; } = new();
}

public class CreateQuizQuestionRequest
{
    public string Content { get; set; } = string.Empty;
    public List<CreateQuizAnswerRequest> Answers { get; set; } = new();
}

public class CreateQuizAnswerRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class UpdateQuizRequest
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<UpdateQuizQuestionRequest> Questions { get; set; } = new();
}

public class UpdateQuizQuestionRequest
{
    public string Content { get; set; } = string.Empty;
    public List<UpdateQuizAnswerRequest> Answers { get; set; } = new();
}

public class UpdateQuizAnswerRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class QuizResponse
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public string Title { get; set; } = string.Empty;
    public int QuestionsCount { get; set; }
    public IReadOnlyCollection<QuizQuestionResponse> Questions { get; set; } = Array.Empty<QuizQuestionResponse>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class QuizQuestionResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public IReadOnlyCollection<QuizAnswerResponse> Answers { get; set; } = Array.Empty<QuizAnswerResponse>();
}

public class QuizAnswerResponse
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class PagedQuizResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public IReadOnlyCollection<QuizResponse> Items { get; set; } = Array.Empty<QuizResponse>();
}
