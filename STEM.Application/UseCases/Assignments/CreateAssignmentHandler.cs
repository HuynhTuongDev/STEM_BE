using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class CreateAssignmentHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;

    public CreateAssignmentHandler(
        IAssignmentRepository assignmentRepository,
        IClassRepository classRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
    }

    public async Task<AssignmentResponse> Handle(
        CreateAssignmentRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        AssignmentRequestMapper.ValidateBase(
            request.ClassId,
            request.Title,
            request.AssignmentType,
            request.Status,
            request.MaxScore);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var classEntity = await _classRepository.GetByIdSummaryAsync(request.ClassId, cancellationToken);
        if (classEntity == null)
        {
            throw new KeyNotFoundException("Class not found.");
        }

        if (!AssignmentAuthorization.CanManageClass(currentUser, classEntity))
        {
            throw new UnauthorizedAccessException("You are not allowed to create assignments for this class.");
        }

        var now = DateTime.UtcNow;
        var assignment = new Assignment();
        AssignmentRequestMapper.ApplyBase(assignment, request, currentUser.Id, now);
        AssignmentRequestMapper.ApplyDetails(assignment, request, now);

        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        assignment.Class = classEntity;

        return AssignmentResponseMapper.Map(assignment);
    }
}
