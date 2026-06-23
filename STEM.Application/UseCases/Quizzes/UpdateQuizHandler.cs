using STEM.Application.Dtos.Quizzes;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Quizzes;

public class UpdateQuizHandler
{
    private readonly IQuizRepository _quizRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;

    public UpdateQuizHandler(
        IQuizRepository quizRepository,
        ICourseRepository courseRepository,
        IUserRepository userRepository)
    {
        _quizRepository = quizRepository;
        _courseRepository = courseRepository;
        _userRepository = userRepository;
    }

    public async Task<QuizResponse> Handle(
        int quizId,
        UpdateQuizRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.CourseId, request.Title, request.Questions);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var quiz = await _quizRepository.GetByIdWithDetailsAsync(quizId, cancellationToken);
        if (quiz == null)
        {
            throw new KeyNotFoundException("Quiz not found.");
        }

        if (quiz.Course == null || !QuizAuthorization.CanManageCourse(currentUser, quiz.Course))
        {
            throw new UnauthorizedAccessException("You are not allowed to update this quiz.");
        }

        var targetCourse = quiz.CourseId == request.CourseId
            ? quiz.Course
            : await _courseRepository.GetCourseDetailAsync(request.CourseId, cancellationToken);

        if (targetCourse == null)
        {
            throw new KeyNotFoundException("Course not found.");
        }

        if (!QuizAuthorization.CanManageCourse(currentUser, targetCourse))
        {
            throw new UnauthorizedAccessException("You are not allowed to move this quiz to the selected course.");
        }

        var now = DateTime.UtcNow;
        var existingQuestions = quiz.QuizQuestions.ToList();
        _quizRepository.DeleteQuestions(existingQuestions);
        quiz.QuizQuestions.Clear();

        quiz.CourseId = request.CourseId;
        quiz.Course = targetCourse;
        quiz.Title = request.Title.Trim();
        quiz.UpdatedAt = now;
        quiz.QuizQuestions = BuildQuestions(request.Questions, now);

        _quizRepository.Update(quiz);
        await _quizRepository.SaveChangesAsync(cancellationToken);

        return QuizResponseMapper.Map(quiz, includeQuestions: true);
    }

    private static void ValidateRequest(
        int courseId,
        string title,
        IEnumerable<UpdateQuizQuestionRequest>? questions)
    {
        if (courseId <= 0)
        {
            throw new ArgumentException("CourseId is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.");
        }

        ValidateQuestions(questions ?? Enumerable.Empty<UpdateQuizQuestionRequest>());
    }

    private static void ValidateQuestions(IEnumerable<UpdateQuizQuestionRequest> questions)
    {
        var questionNumber = 1;
        foreach (var question in questions)
        {
            if (string.IsNullOrWhiteSpace(question.Content))
            {
                throw new ArgumentException($"Question {questionNumber} content is required.");
            }

            var answerNumber = 1;
            foreach (var answer in question.Answers)
            {
                if (string.IsNullOrWhiteSpace(answer.Content))
                {
                    throw new ArgumentException($"Question {questionNumber} answer {answerNumber} content is required.");
                }

                answerNumber++;
            }

            questionNumber++;
        }
    }

    private static List<QuizQuestion> BuildQuestions(
        IEnumerable<UpdateQuizQuestionRequest> questions,
        DateTime now)
    {
        return questions.Select(question => new QuizQuestion
        {
            Content = question.Content.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            QuizAnswers = question.Answers.Select(answer => new QuizAnswer
            {
                Content = answer.Content.Trim(),
                IsCorrect = answer.IsCorrect,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList()
        }).ToList();
    }
}
