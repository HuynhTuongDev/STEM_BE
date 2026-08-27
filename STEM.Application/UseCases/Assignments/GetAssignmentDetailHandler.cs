using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class GetAssignmentDetailHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IUserRepository _userRepository;

    public GetAssignmentDetailHandler(
        IAssignmentRepository assignmentRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _userRepository = userRepository;
    }

    public async Task<AssignmentResponse> Handle(
        int assignmentId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var assignment = await _assignmentRepository.GetByIdWithDetailsAsync(assignmentId, cancellationToken);
        if (assignment == null)
        {
            throw new KeyNotFoundException("Assignment not found.");
        }

        if (!AssignmentAuthorization.CanViewAssignment(currentUser, assignment))
        {
            throw new UnauthorizedAccessException("You are not allowed to view this assignment.");
        }

        var revealAnswers = currentUser.Role?.Name != RoleNames.Student;
        return AssignmentResponseMapper.Map(assignment, classEntityFromParam: assignment.Class, revealAnswers: revealAnswers);
    }
}
