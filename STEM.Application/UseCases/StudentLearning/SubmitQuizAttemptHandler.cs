using STEM.Application.Dtos.StudentLearning;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.StudentLearning;

public class SubmitQuizAttemptHandler
{
    private readonly IQuizRepository _quizRepository;
    private readonly IQuizAttemptRepository _quizAttemptRepository;
    private readonly IUserRepository _userRepository;

    public SubmitQuizAttemptHandler(
        IQuizRepository quizRepository,
        IQuizAttemptRepository quizAttemptRepository,
        IUserRepository userRepository)
    {
        _quizRepository = quizRepository;
        _quizAttemptRepository = quizAttemptRepository;
        _userRepository = userRepository;
    }

    public async Task<StudentQuizResultResponse> Handle(
        int quizId,
        SubmitQuizAttemptRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        StudentLearningGuard.EnsureStudent(currentUser);

        if (request.Answers.GroupBy(a => a.QuestionId).Any(g => g.Count() > 1))
        {
            throw new ArgumentException("Each question can only have one submitted answer.");
        }

        var quiz = await _quizRepository.GetByIdWithDetailsAsync(quizId, cancellationToken);
        if (quiz == null)
        {
            throw new KeyNotFoundException("Quiz not found.");
        }

        if (!StudentLearningGuard.CanAccessQuiz(quiz, currentUserId))
        {
            throw new UnauthorizedAccessException("Student is not enrolled in this quiz course.");
        }

        var questions = quiz.QuizQuestions.OrderBy(q => q.Id).ToList();
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("Quiz has no questions.");
        }

        var submittedAnswers = request.Answers.ToDictionary(a => a.QuestionId);
        var unknownQuestionId = submittedAnswers.Keys.FirstOrDefault(id => questions.All(q => q.Id != id));
        if (unknownQuestionId != 0)
        {
            throw new ArgumentException($"Question {unknownQuestionId} does not belong to this quiz.");
        }

        var attemptAnswers = new List<QuizAttemptAnswer>();
        var correctAnswers = 0;

        foreach (var question in questions)
        {
            submittedAnswers.TryGetValue(question.Id, out var submittedAnswer);
            QuizAnswer? selectedAnswer = null;

            if (submittedAnswer?.AnswerId != null)
            {
                selectedAnswer = question.QuizAnswers.FirstOrDefault(a => a.Id == submittedAnswer.AnswerId.Value);
                if (selectedAnswer == null)
                {
                    throw new ArgumentException($"Answer {submittedAnswer.AnswerId.Value} does not belong to question {question.Id}.");
                }
            }

            var isCorrect = selectedAnswer?.IsCorrect == true;
            if (isCorrect)
            {
                correctAnswers++;
            }

            attemptAnswers.Add(new QuizAttemptAnswer
            {
                QuestionId = question.Id,
                AnswerId = selectedAnswer?.Id,
                IsCorrect = isCorrect
            });
        }

        var now = DateTime.UtcNow;
        var totalQuestions = questions.Count;
        var score = Math.Round(correctAnswers * 100m / totalQuestions, 2, MidpointRounding.AwayFromZero);

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            StudentId = currentUserId,
            TotalQuestions = totalQuestions,
            CorrectAnswers = correctAnswers,
            Score = score,
            StartedAt = now,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            Answers = attemptAnswers
        };

        await _quizAttemptRepository.AddAsync(attempt, cancellationToken);
        await _quizAttemptRepository.SaveChangesAsync(cancellationToken);

        var savedAttempt = await _quizAttemptRepository.GetByIdWithDetailsAsync(attempt.Id, cancellationToken);
        return StudentLearningMapper.ToQuizResultResponse(savedAttempt ?? attempt);
    }
}
