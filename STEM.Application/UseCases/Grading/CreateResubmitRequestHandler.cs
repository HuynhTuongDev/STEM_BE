using STEM.Application.Dtos.Grading;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class CreateResubmitRequestHandler
{
    private readonly IResubmitRequestRepository _resubmitRequestRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateResubmitRequestHandler(
        IResubmitRequestRepository resubmitRequestRepository,
        IAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _resubmitRequestRepository = resubmitRequestRepository;
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ResubmitRequestResponse> Handle(
        CreateResubmitRequestRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null || currentUser.Role?.Name != RoleNames.Student)
        {
            throw new UnauthorizedAccessException("Only students can request a resubmission.");
        }

        var assignment = await _assignmentRepository.GetByIdWithDetailsAsync(request.AssignmentId, cancellationToken);
        if (assignment == null)
        {
            throw new KeyNotFoundException("Assignment not found.");
        }

        var isEnrolled = assignment.Class?.Enrollments.Any(enrollment => enrollment.StudentId == currentUser.Id) == true;
        if (!isEnrolled)
        {
            throw new UnauthorizedAccessException("You are not enrolled in this assignment's class.");
        }

        var existingPending = await _resubmitRequestRepository.GetPendingAsync(
            assignment.Id, currentUser.Id, cancellationToken);
        if (existingPending != null)
        {
            throw new InvalidOperationException("Bạn đã có 1 yêu cầu đang chờ giáo viên duyệt cho bài này.");
        }

        var submissions = await _submissionRepository.GetByAssignmentIdAsync(assignment.Id, cancellationToken);
        var existingCount = submissions.Count(item => item.StudentId == currentUser.Id);

        var approvedRequests = (await _resubmitRequestRepository.GetFilteredAsync(
                assignment.Id, ResubmitRequestStatuses.Approved, currentUser.Id, null, null, cancellationToken))
            .ToList();

        var isBlocked = ResubmitEligibility.IsBlocked(
            assignment, existingCount, approvedRequests, _dateTimeProvider.UtcNow, out _);
        if (!isBlocked)
        {
            throw new InvalidOperationException("Bạn vẫn còn quyền nộp bài cho assignment này — không cần gửi yêu cầu.");
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        if (reason != null && reason.Length > 1000)
        {
            throw new ArgumentException("Lý do không được vượt quá 1000 ký tự.");
        }

        var resubmitRequest = new ResubmitRequest
        {
            AssignmentId = assignment.Id,
            StudentId = currentUser.Id,
            Reason = reason,
            Status = ResubmitRequestStatuses.Pending
        };

        await _resubmitRequestRepository.AddAsync(resubmitRequest, cancellationToken);
        await _resubmitRequestRepository.SaveChangesAsync(cancellationToken);

        if (assignment.Class != null)
        {
            await _notificationRepository.AddAsync(new Notification
            {
                UserId = assignment.Class.TeacherId,
                Title = "Yêu cầu nộp lại",
                Content = $"{currentUser.FullName} xin phép nộp lại bài \"{assignment.Title}\".",
                Type = NotificationType.ResubmitRequest
            }, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }

        var saved = await _resubmitRequestRepository.GetByIdWithDetailsAsync(resubmitRequest.Id, cancellationToken)
            ?? resubmitRequest;
        return ResubmitRequestResponseMapper.Map(saved);
    }
}
