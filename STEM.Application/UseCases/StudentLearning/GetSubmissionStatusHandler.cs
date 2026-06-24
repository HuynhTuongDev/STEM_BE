using STEM.Application.Dtos.StudentLearning;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.StudentLearning;

public class GetSubmissionStatusHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;

    public GetSubmissionStatusHandler(
        IAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
    }

    public async Task<SubmissionStatusResponse> Handle(
        int assignmentId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        StudentLearningGuard.EnsureStudent(currentUser);

        var assignment = await _assignmentRepository.GetByIdWithDetailsAsync(assignmentId, cancellationToken);
        if (assignment == null)
        {
            throw new KeyNotFoundException("Assignment not found.");
        }

        if (!StudentLearningGuard.CanAccessAssignment(assignment, currentUserId))
        {
            throw new UnauthorizedAccessException("Student is not enrolled in this assignment class.");
        }

        var submission = await _submissionRepository.GetByAssignmentAndStudentAsync(
            assignmentId,
            currentUserId,
            cancellationToken);

        return StudentLearningMapper.ToSubmissionStatus(assignment, submission);
    }
}
