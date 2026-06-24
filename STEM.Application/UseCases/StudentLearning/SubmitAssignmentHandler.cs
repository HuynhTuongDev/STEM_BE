using STEM.Application.Dtos.StudentLearning;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.StudentLearning;

public class SubmitAssignmentHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;

    public SubmitAssignmentHandler(
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
        SubmitAssignmentRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        StudentLearningGuard.EnsureStudent(currentUser);

        if (string.IsNullOrWhiteSpace(request.FileUrl))
        {
            throw new ArgumentException("FileUrl is required.");
        }

        var assignment = await _assignmentRepository.GetByIdWithDetailsAsync(assignmentId, cancellationToken);
        if (assignment == null)
        {
            throw new KeyNotFoundException("Assignment not found.");
        }

        if (!StudentLearningGuard.CanAccessAssignment(assignment, currentUserId))
        {
            throw new UnauthorizedAccessException("Student is not enrolled in this assignment class.");
        }

        var now = DateTime.UtcNow;
        var submission = await _submissionRepository.GetByAssignmentAndStudentAsync(
            assignmentId,
            currentUserId,
            cancellationToken);

        if (submission == null)
        {
            submission = new Submission
            {
                AssignmentId = assignmentId,
                StudentId = currentUserId,
                File = new FileEntity
                {
                    Url = request.FileUrl.Trim(),
                    CreatedAt = now,
                    UpdatedAt = now
                },
                CreatedAt = now,
                UpdatedAt = now
            };

            await _submissionRepository.AddAsync(submission, cancellationToken);
        }
        else
        {
            submission.File = new FileEntity
            {
                Url = request.FileUrl.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };
            submission.Score = null;
            submission.Feedback = null;
            submission.GradedById = null;
            submission.GradedAt = null;
            submission.UpdatedAt = now;
            _submissionRepository.Update(submission);
        }

        await _submissionRepository.SaveChangesAsync(cancellationToken);

        var savedSubmission = await _submissionRepository.GetByAssignmentAndStudentAsync(
            assignmentId,
            currentUserId,
            cancellationToken);

        return StudentLearningMapper.ToSubmissionStatus(assignment, savedSubmission);
    }
}
