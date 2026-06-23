using STEM.Application.Dtos.Quizzes;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Quizzes;

public class CreateQuizHandler
{
    private readonly IQuizRepository _quizRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;

    public CreateQuizHandler(
        IQuizRepository quizRepository,
        ICourseRepository courseRepository,
        IUserRepository userRepository)
    {
        _quizRepository = quizRepository;
        _courseRepository = courseRepository;
        _userRepository = userRepository;
    }

    public async Task<QuizResponse> Handle(
        CreateQuizRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.CourseId, request.Title, request.Questions);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var course = await _courseRepository.GetCourseDetailAsync(request.CourseId, cancellationToken);
        if (course == null)
        {
            throw new KeyNotFoundException("Course not found.");
        }

        if (!QuizAuthorization.CanManageCourse(currentUser, course))
        {
            throw new UnauthorizedAccessException("You are not allowed to create quizzes for this course.");
        }

        var now = DateTime.UtcNow;
        var quiz = new Quiz
        {
            CourseId = request.CourseId,
            Course = course,
            Title = request.Title.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            QuizQuestions = BuildQuestions(request.Questions, now)
        };

        await _quizRepository.AddAsync(quiz, cancellationToken);
        await _quizRepository.SaveChangesAsync(cancellationToken);

        return QuizResponseMapper.Map(quiz, includeQuestions: true);
    }

    private static void ValidateRequest(
        int courseId,
        string title,
        IEnumerable<CreateQuizQuestionRequest>? questions)
    {
        if (courseId <= 0)
        {
            throw new ArgumentException("CourseId is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.");
        }

        ValidateQuestions(questions ?? Enumerable.Empty<CreateQuizQuestionRequest>());
    }

    private static void ValidateQuestions(IEnumerable<CreateQuizQuestionRequest> questions)
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
        IEnumerable<CreateQuizQuestionRequest> questions,
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
