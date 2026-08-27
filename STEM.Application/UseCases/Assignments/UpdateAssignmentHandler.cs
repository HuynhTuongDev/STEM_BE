using System.Text.Json;
using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Assessments;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class UpdateAssignmentHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRubricRepository _rubricRepository;

    public UpdateAssignmentHandler(
        IAssignmentRepository assignmentRepository,
        IClassRepository classRepository,
        IUserRepository userRepository,
        IRubricRepository rubricRepository)
    {
        _assignmentRepository = assignmentRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
        _rubricRepository = rubricRepository;
    }

    public async Task<AssignmentResponse> Handle(
        int assignmentId,
        UpdateAssignmentRequest request,
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

        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId, cancellationToken);
        if (assignment == null)
        {
            throw new KeyNotFoundException("Assignment not found.");
        }

        var currentClass = await _classRepository.GetByIdSummaryAsync(assignment.ClassId, cancellationToken);
        if (currentClass == null || !AssignmentAuthorization.CanManageClass(currentUser, currentClass))
        {
            throw new UnauthorizedAccessException("You are not allowed to update this assignment.");
        }

        var targetClass = assignment.ClassId == request.ClassId
            ? currentClass
            : await _classRepository.GetByIdSummaryAsync(request.ClassId, cancellationToken);

        if (targetClass == null)
        {
            throw new KeyNotFoundException("Class not found.");
        }

        if (!AssignmentAuthorization.CanManageClass(currentUser, targetClass))
        {
            throw new UnauthorizedAccessException("You are not allowed to move this assignment to the selected class.");
        }

        var now = DateTime.UtcNow;
        AssignmentRequestMapper.ApplyBase(assignment, request, now);
        await _assignmentRepository.DeleteDetailsAsync(assignment.Id, cancellationToken);
        AssignmentRequestMapper.ApplyDetails(assignment, request, now);

        // Delete old Rubric and create new if criteria provided
        await _rubricRepository.DeleteByAssignmentIdAsync(assignment.Id, cancellationToken);

        if (request.RubricCriteria != null && request.RubricCriteria.Count > 0)
        {
            var rubric = new Rubric
            {
                AssignmentId = assignment.Id,
                Criteria = JsonSerializer.Serialize(request.RubricCriteria),
                MaxScore = request.RubricCriteria.Sum(c => c.MaxPoints),
                CreatedAt = now,
                UpdatedAt = now
            };
            await _rubricRepository.AddAsync(rubric, cancellationToken);
        }

        _assignmentRepository.Update(assignment);
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        // Reload assignment to get the new RubricId
        var updatedAssignment = await _assignmentRepository.GetByIdAsync(assignment.Id, cancellationToken);
        return AssignmentResponseMapper.Map(updatedAssignment!, targetClass);
    }
}
