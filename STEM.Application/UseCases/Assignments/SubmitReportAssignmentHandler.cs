using System.Text.Json;
using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class SubmitReportAssignmentHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;

    public SubmitReportAssignmentHandler(
        IAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
    }

    public async Task<SubmitReportResponse> Handle(
        int assignmentId,
        SubmitReportRequest request,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _assignmentRepository.GetByIdWithDetailsAsync(assignmentId, cancellationToken);
        if (assignment == null)
            throw new KeyNotFoundException("Assignment not found.");

        if (!string.Equals(assignment.AssignmentType, AssignmentTypes.TextReport, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("This assignment is not a text report.");

        if (assignment.Status != AssignmentStatuses.Published)
            throw new InvalidOperationException("Assignment is not published.");

        if (assignment.DueDate.HasValue && assignment.DueDate.Value < DateTime.UtcNow)
            throw new InvalidOperationException("Assignment deadline has passed.");

        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
            throw new UnauthorizedAccessException("Student not found.");

        var attemptCount = await _submissionRepository.GetAttemptCountAsync(assignmentId, studentId, cancellationToken);

        if (!assignment.AllowResubmit && attemptCount > 0)
            throw new InvalidOperationException("Resubmission is not allowed for this assignment.");

        if (assignment.ResubmitLimit.HasValue && attemptCount >= assignment.ResubmitLimit.Value)
            throw new InvalidOperationException($"You have reached the maximum number of attempts ({assignment.ResubmitLimit.Value}).");

        var contentJson = JsonSerializer.Serialize(new
        {
            content = request.Content,
            fileId = request.FileId
        });

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatuses.Submitted,
            ContentJson = contentJson,
            FileId = request.FileId,
            AttemptNumber = attemptCount + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _submissionRepository.AddAsync(submission, cancellationToken);
        await _submissionRepository.SaveChangesAsync(cancellationToken);

        return new SubmitReportResponse
        {
            SubmissionId = submission.Id,
            AttemptNumber = submission.AttemptNumber,
            Status = submission.Status,
            SubmittedAt = submission.SubmittedAt ?? DateTime.UtcNow
        };
    }
}
