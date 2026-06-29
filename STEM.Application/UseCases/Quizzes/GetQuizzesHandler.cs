using STEM.Application.Dtos.Quizzes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Quizzes;

public class GetQuizzesHandler
{
    private readonly IQuizRepository _quizRepository;
    private readonly IUserRepository _userRepository;

    public GetQuizzesHandler(
        IQuizRepository quizRepository,
        IUserRepository userRepository)
    {
        _quizRepository = quizRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedQuizResponse> Handle(
        GetQuizzesRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        int? schoolId = null;
        int? teacherId = null;
        int? studentId = null;
        var roleName = currentUser.Role?.Name;

        if (roleName == RoleNames.SchoolAdministrator)
        {
            schoolId = currentUser.SchoolId ?? throw new UnauthorizedAccessException("School admin has no school.");
        }
        else if (roleName == RoleNames.Teacher)
        {
            teacherId = currentUser.Id;
        }
        else if (roleName == RoleNames.Student)
        {
            studentId = currentUser.Id;
        }
        else
        {
            throw new UnauthorizedAccessException("You are not allowed to view quizzes.");
        }

        var (quizzes, totalCount) = await _quizRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            request.CourseId,
            schoolId,
            teacherId,
            studentId,
            cancellationToken);

        return new PagedQuizResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = quizzes.Select(quiz => QuizResponseMapper.Map(quiz, includeQuestions: false)).ToList()
        };
    }
}
