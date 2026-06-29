using STEM.Application.Dtos.Assignments;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class UpdateAssignmentHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;

    public UpdateAssignmentHandler(
        IAssignmentRepository assignmentRepository,
        IClassRepository classRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
    }

    public async Task<AssignmentResponse> Handle(
        int assignmentId,
        UpdateAssignmentRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request.ClassId, request.Title);

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
            throw new UnauthorizedAccessException("You are not allowed to update this assignment.");
        }

        var targetClass = assignment.ClassId == request.ClassId
            ? assignment.Class
            : await _classRepository.GetByIdWithDetailsAsync(request.ClassId, cancellationToken);

        if (targetClass == null)
        {
            throw new KeyNotFoundException("Class not found.");
        }

        if (!AssignmentAuthorization.CanManageClass(currentUser, targetClass))
        {
            throw new UnauthorizedAccessException("You are not allowed to move this assignment to the selected class.");
        }

        assignment.ClassId = request.ClassId;
        assignment.Class = targetClass;
        assignment.Title = request.Title.Trim();
        assignment.UpdatedAt = DateTime.UtcNow;

        _assignmentRepository.Update(assignment);
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        return AssignmentResponseMapper.Map(assignment);
    }

    private static void ValidateRequest(int classId, string title)
    {
        if (classId <= 0)
        {
            throw new ArgumentException("ClassId is required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.");
        }
    }
}
