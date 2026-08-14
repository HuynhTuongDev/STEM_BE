using System.Text.Json;
using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class SubmitQuizAssignmentHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;

    public SubmitQuizAssignmentHandler(
        IAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
    }

    public async Task<SubmitQuizResponse> Handle(
        int assignmentId,
        SubmitQuizRequest request,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _assignmentRepository.GetByIdWithDetailsAsync(assignmentId, cancellationToken);
        if (assignment == null)
            throw new KeyNotFoundException("Assignment not found.");

        if (!string.Equals(assignment.AssignmentType, AssignmentTypes.Quiz, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("This assignment is not a quiz.");

        // Load QuizDetail directly to ensure it's available
        var quizDetail = await _assignmentRepository.GetQuizDetailAsync(assignmentId, cancellationToken);
        if (quizDetail == null)
            throw new InvalidOperationException("Quiz detail not found.");

        if (assignment.Status != AssignmentStatuses.Published)
            throw new InvalidOperationException("Assignment is not published.");

        if (assignment.DueDate.HasValue && assignment.DueDate.Value < DateTime.UtcNow)
            throw new InvalidOperationException("Assignment deadline has passed.");

        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
            throw new UnauthorizedAccessException("Student not found.");

        var attemptCount = await _submissionRepository.GetAttemptCountAsync(assignmentId, studentId, cancellationToken);

        if (!assignment.AllowResubmit && attemptCount > 0)
            throw new InvalidOperationException("Resubmission is not allowed for this assignment.");

        if (assignment.ResubmitLimit.HasValue && attemptCount >= assignment.ResubmitLimit.Value)
            throw new InvalidOperationException($"You have reached the maximum number of attempts ({assignment.ResubmitLimit.Value}).");

        var questions = ParseQuestions(quizDetail.QuestionsJson);
        var results = new List<QuizAnswerResult>();
        var correctCount = 0;

        foreach (var answer in request.Answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) continue;

            var isCorrect = false;
            object? correctAnswer = null;

            switch (question.Type?.ToLowerInvariant())
            {
                case "single_choice":
                    var correctSingleOption = question.Options?.FirstOrDefault(o => o.IsCorrect);
                    if (correctSingleOption != null)
                    {
                        isCorrect = answer.Answer?.ToString() == correctSingleOption.Id;
                        correctAnswer = correctSingleOption.Id;
                    }
                    break;

                case "multiple_choice":
                    var correctMultipleOptions = question.Options?.Where(o => o.IsCorrect).Select(o => o.Id).ToList() ?? new List<string>();
                    var studentAnswers = NormalizeAnswers(answer.Answer);
                    var expectedAnswers = correctMultipleOptions;

                    isCorrect = studentAnswers.Count == expectedAnswers.Count &&
                                studentAnswers.All(a => expectedAnswers.Contains(a));
                    correctAnswer = correctMultipleOptions;
                    break;

                case "fill_blank":
                    var correctOptions = question.Options?.Where(o => o.IsCorrect).ToList() ?? new List<QuizOption>();
                    var correctText = correctOptions.FirstOrDefault()?.Text ?? question.CorrectAnswer ?? "";
                    var studentText = NormalizeTextAnswer(answer.Answer?.ToString() ?? "");
                    isCorrect = string.Equals(studentText, correctText, StringComparison.OrdinalIgnoreCase);
                    correctAnswer = correctText;
                    break;
            }

            if (isCorrect) correctCount++;

            results.Add(new QuizAnswerResult
            {
                QuestionId = answer.QuestionId,
                IsCorrect = isCorrect,
                StudentAnswer = answer.Answer,
                CorrectAnswer = correctAnswer
            });
        }

        var score = assignment.MaxScore * correctCount / Math.Max(questions.Count, 1);

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatuses.Graded,
            ContentJson = JsonSerializer.Serialize(request.Answers),
            AutoGradeResultJson = JsonSerializer.Serialize(results),
            AutoScore = score,
            FinalScore = score,
            Score = score,
            AttemptNumber = attemptCount + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            GradedAt = DateTime.UtcNow
        };

        await _submissionRepository.AddAsync(submission, cancellationToken);
        await _submissionRepository.SaveChangesAsync(cancellationToken);

        return new SubmitQuizResponse
        {
            SubmissionId = submission.Id,
            AttemptNumber = submission.AttemptNumber,
            Score = score,
            MaxScore = assignment.MaxScore,
            CorrectCount = correctCount,
            TotalQuestions = questions.Count,
            IsAutoGraded = true,
            Results = results
        };
    }

    private List<QuizQuestion> ParseQuestions(string questionsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<List<QuizQuestion>>(questionsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<QuizQuestion>();
        }
        catch
        {
            return new List<QuizQuestion>();
        }
    }

    private List<string> NormalizeAnswers(object? answer)
    {
        if (answer == null) return new List<string>();

        if (answer is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Array)
                return jsonElement.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
            if (jsonElement.ValueKind == JsonValueKind.String)
                return new List<string> { jsonElement.GetString() ?? "" };
        }

        if (answer is IEnumerable<string> stringList)
            return stringList.ToList();

        if (answer is IEnumerable<object> objectList)
            return objectList.Select(o => o?.ToString() ?? "").ToList();

        return new List<string> { answer.ToString() ?? "" };
    }

    private string NormalizeTextAnswer(string answer)
    {
        return answer.Trim().ToLowerInvariant();
    }

    private class QuizQuestion
    {
        public string Id { get; set; } = "";
        public string? Type { get; set; }
        public string? Text { get; set; }
        public List<QuizOption>? Options { get; set; }
        public string? CorrectAnswer { get; set; }
    }

    private class QuizOption
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public bool IsCorrect { get; set; }
    }
}
