using STEM.Application.Dtos.Grading;
using STEM.Core.Entities.Common;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class GradeSubmissionHandler
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;

    public GradeSubmissionHandler(
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository)
    {
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<SubmissionResponse> Handle(
        int submissionId,
        GradeSubmissionRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var submission = await _submissionRepository.GetByIdWithDetailsAsync(submissionId, cancellationToken);
        if (submission == null)
        {
            throw new KeyNotFoundException("Submission not found.");
        }

        if (!SubmissionAuthorization.CanManageSubmission(currentUser, submission))
        {
            throw new UnauthorizedAccessException("You are not allowed to grade this submission.");
        }

        if (submission.Score.HasValue)
        {
            throw new InvalidOperationException("Submission is already graded. Use update grade instead.");
        }

        ApplyGrade(submission, request, currentUser.Id);
        submission.GradedBy = currentUser;

        _submissionRepository.Update(submission);
        await _submissionRepository.SaveChangesAsync(cancellationToken);

        if (submission.StudentId.HasValue)
        {
            await _notificationRepository.AddAsync(new Notification
            {
                UserId = submission.StudentId.Value,
                Title = "Bài đã được chấm",
                Content = $"\"{submission.Assignment?.Title}\" đã được chấm — {submission.Score} điểm.",
                Type = "GradeReport"
            }, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }

        return SubmissionResponseMapper.Map(submission);
    }

    internal static void ValidateRequest(GradeSubmissionRequest request)
    {
        if (request.Score < 0 || request.Score > 100)
        {
            throw new ArgumentException("Score must be between 0 and 100.");
        }
    }

    internal static void ApplyGrade(
        STEM.Core.Entities.Projects.Submission submission,
        GradeSubmissionRequest request,
        int gradedById)
    {
        submission.Score = request.Score;
        submission.Feedback = string.IsNullOrWhiteSpace(request.Feedback) ? null : request.Feedback.Trim();
        submission.GradedById = gradedById;
        submission.GradedAt = DateTime.UtcNow;
        submission.UpdatedAt = DateTime.UtcNow;
        submission.Status = "graded";
    }
}
