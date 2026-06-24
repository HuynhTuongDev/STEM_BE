using STEM.Application.Dtos.StudentLearning;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.StudentLearning;

public class GetStudentQuizzesHandler
{
    private readonly IQuizRepository _quizRepository;
    private readonly IQuizAttemptRepository _quizAttemptRepository;
    private readonly IUserRepository _userRepository;

    public GetStudentQuizzesHandler(
        IQuizRepository quizRepository,
        IQuizAttemptRepository quizAttemptRepository,
        IUserRepository userRepository)
    {
        _quizRepository = quizRepository;
        _quizAttemptRepository = quizAttemptRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedStudentQuizResponse> Handle(
        GetStudentQuizzesRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        StudentLearningGuard.EnsureStudent(currentUser);

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var (quizzes, totalCount) = await _quizRepository.GetStudentQuizzesPagedAsync(
            currentUserId,
            pageNumber,
            pageSize,
            request.CourseId,
            request.SearchTerm,
            cancellationToken);

        var items = new List<StudentQuizListItemResponse>();
        foreach (var quiz in quizzes)
        {
            var latestAttempt = await _quizAttemptRepository.GetLatestByQuizAndStudentAsync(
                quiz.Id,
                currentUserId,
                cancellationToken);

            items.Add(new StudentQuizListItemResponse
            {
                Id = quiz.Id,
                Title = quiz.Title,
                CourseId = quiz.CourseId,
                CourseTitle = quiz.Course?.Title ?? string.Empty,
                TeacherId = quiz.Course?.TeacherId ?? 0,
                TeacherName = quiz.Course?.Teacher?.FullName ?? string.Empty,
                QuestionCount = quiz.QuizQuestions.Count,
                HasAttempt = latestAttempt != null,
                LatestScore = latestAttempt?.Score,
                LatestSubmittedAt = latestAttempt?.SubmittedAt,
                CreatedAt = quiz.CreatedAt
            });
        }

        return new PagedStudentQuizResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }
}
