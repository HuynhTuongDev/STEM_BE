using STEM.Application.Dtos.Grading;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class GetSubmissionsHandler
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;

    public GetSubmissionsHandler(
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository)
    {
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedSubmissionResponse> Handle(
        GetSubmissionsRequest request,
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
        var studentId = request.StudentId;
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
            if (request.StudentId.HasValue && request.StudentId.Value != currentUser.Id)
            {
                throw new UnauthorizedAccessException("Students can only view their own submissions.");
            }

            studentId = currentUser.Id;
        }
        else
        {
            throw new UnauthorizedAccessException("You are not allowed to view submissions.");
        }

        var (submissions, totalCount) = await _submissionRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            request.AssignmentId,
            request.ClassId,
            studentId,
            schoolId,
            teacherId,
            cancellationToken);

        return new PagedSubmissionResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = submissions.Select(SubmissionResponseMapper.Map).ToList()
        };
    }
}
