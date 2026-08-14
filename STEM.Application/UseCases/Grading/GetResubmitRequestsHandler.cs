using STEM.Application.Dtos.Grading;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class GetResubmitRequestsHandler
{
    private readonly IResubmitRequestRepository _resubmitRequestRepository;
    private readonly IUserRepository _userRepository;

    public GetResubmitRequestsHandler(
        IResubmitRequestRepository resubmitRequestRepository,
        IUserRepository userRepository)
    {
        _resubmitRequestRepository = resubmitRequestRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<ResubmitRequestResponse>> Handle(
        GetResubmitRequestsQuery query,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

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
            throw new UnauthorizedAccessException("You are not allowed to view resubmit requests.");
        }

        var requests = await _resubmitRequestRepository.GetFilteredAsync(
            query.AssignmentId, query.Status, studentId, teacherId, schoolId, cancellationToken);

        return requests.Select(ResubmitRequestResponseMapper.Map).ToList();
    }
}
