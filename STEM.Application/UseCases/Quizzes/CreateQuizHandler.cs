using STEM.Application.Dtos.Quizzes;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Quizzes;

public class CreateQuizHandler
{
    private readonly IQuizRepository _quizRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;

    public CreateQuizHandler(
        IQuizRepository quizRepository,
        IClassRepository classRepository,
        IUserRepository userRepository)
    {
        _quizRepository = quizRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
    }

    public async Task<QuizResponse> Handle(
        CreateQuizRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.ClassId, request.Title, request.Questions);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var classEntity = await _classRepository.GetByIdWithDetailsAsync(request.ClassId, cancellationToken);
        if (classEntity == null)
        {
            throw new KeyNotFoundException("Class not found.");
        }

        if (!QuizAuthorization.CanManageClass(currentUser, classEntity))
        {
            throw new UnauthorizedAccessException("You are not allowed to create quizzes for this class.");
        }

        var now = DateTime.UtcNow;
        var quiz = new Quiz
        {
            ClassId = request.ClassId,
            Class = classEntity,
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
        int classId,
        string title,
        IEnumerable<CreateQuizQuestionRequest>? questions)
    {
        if (classId <= 0)
        {
            throw new ArgumentException("ClassId is required.");
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
