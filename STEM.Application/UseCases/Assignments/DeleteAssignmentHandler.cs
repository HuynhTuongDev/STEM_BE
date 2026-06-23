using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class DeleteAssignmentHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IUserRepository _userRepository;

    public DeleteAssignmentHandler(
        IAssignmentRepository assignmentRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(
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

        if (assignment.Class == null || !AssignmentAuthorization.CanManageClass(currentUser, assignment.Class))
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this assignment.");
        }

        _assignmentRepository.Delete(assignment);
        await _assignmentRepository.SaveChangesAsync(cancellationToken);
    }
}
