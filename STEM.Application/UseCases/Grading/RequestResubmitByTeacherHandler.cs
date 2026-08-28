using STEM.Application.Dtos.Grading;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

// Phase 13 (teacher-initiated resubmit): giáo viên xem 1 Submission cụ thể và
// bấm "Yêu cầu nộp lại" — khác hẳn CreateResubmitRequestHandler (học sinh xin
// phép khi đã hết lượt). Reuse 100% bảng ResubmitRequests, không thêm field/
// bảng mới — chỉ thêm 1 code path tạo request Approved thẳng.
public class RequestResubmitByTeacherHandler
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IResubmitRequestRepository _resubmitRequestRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RequestResubmitByTeacherHandler(
        ISubmissionRepository submissionRepository,
        IResubmitRequestRepository resubmitRequestRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _submissionRepository = submissionRepository;
        _resubmitRequestRepository = resubmitRequestRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ResubmitRequestResponse> Handle(
        int submissionId,
        TeacherRequestResubmitRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
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
            throw new UnauthorizedAccessException("You are not allowed to request a resubmission for this submission.");
        }

        if (!submission.StudentId.HasValue)
        {
            throw new InvalidOperationException("Submission has no associated student.");
        }

        var extraAttempts = request.ExtraAttempts ?? 1;
        if (extraAttempts <= 0)
        {
            throw new ArgumentException("Số lần nộp thêm phải lớn hơn 0.");
        }

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note != null && note.Length > 1000)
        {
            throw new ArgumentException("Ghi chú không được vượt quá 1000 ký tự.");
        }

        var resubmitRequest = new ResubmitRequest
        {
            AssignmentId = submission.AssignmentId,
            StudentId = submission.StudentId.Value,
            Status = ResubmitRequestStatuses.Approved,
            GrantedExtraAttempts = extraAttempts,
            ReviewNote = note,
            ReviewedById = currentUser.Id,
            ReviewedAt = _dateTimeProvider.UtcNow
        };

        await _resubmitRequestRepository.AddAsync(resubmitRequest, cancellationToken);
        await _resubmitRequestRepository.SaveChangesAsync(cancellationToken);

        await _notificationRepository.AddAsync(new Notification
        {
            UserId = submission.StudentId.Value,
            Title = "Giáo viên yêu cầu nộp lại",
            Content = note != null
                ? $"Giáo viên yêu cầu bạn nộp lại bài \"{submission.Assignment?.Title}\": {note}"
                : $"Giáo viên yêu cầu bạn nộp lại bài \"{submission.Assignment?.Title}\".",
            Type = NotificationType.ResubmissionApproved
        }, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        var saved = await _resubmitRequestRepository.GetByIdWithDetailsAsync(resubmitRequest.Id, cancellationToken)
            ?? resubmitRequest;
        return ResubmitRequestResponseMapper.Map(saved);
    }
}
