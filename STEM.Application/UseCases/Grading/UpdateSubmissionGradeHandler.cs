using STEM.Application.Dtos.Grading;
using STEM.Core.Entities.Common;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class UpdateSubmissionGradeHandler
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;

    public UpdateSubmissionGradeHandler(
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
        GradeSubmissionHandler.ValidateRequest(request);

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
            throw new UnauthorizedAccessException("You are not allowed to update this submission grade.");
        }

        GradeSubmissionHandler.ApplyGrade(submission, request, currentUser.Id);
        submission.GradedBy = currentUser;

        _submissionRepository.Update(submission);
        await _submissionRepository.SaveChangesAsync(cancellationToken);

        if (submission.StudentId.HasValue)
        {
            await _notificationRepository.AddAsync(new Notification
            {
                UserId = submission.StudentId.Value,
                Title = "Điểm đã được cập nhật",
                Content = $"\"{submission.Assignment?.Title}\" vừa được chấm lại — {submission.Score} điểm.",
                Type = "GradeReport"
            }, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }

        return SubmissionResponseMapper.Map(submission);
    }
}
