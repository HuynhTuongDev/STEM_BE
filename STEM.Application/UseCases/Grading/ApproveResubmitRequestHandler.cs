using STEM.Application.Dtos.Grading;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class ApproveResubmitRequestHandler
{
    private readonly IResubmitRequestRepository _resubmitRequestRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApproveResubmitRequestHandler(
        IResubmitRequestRepository resubmitRequestRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _resubmitRequestRepository = resubmitRequestRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ResubmitRequestResponse> Handle(
        int id,
        ReviewResubmitRequestRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var resubmitRequest = await _resubmitRequestRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (resubmitRequest == null)
        {
            throw new KeyNotFoundException("Resubmit request not found.");
        }

        if (!ResubmitRequestAuthorization.CanReview(currentUser, resubmitRequest))
        {
            throw new UnauthorizedAccessException("You are not allowed to review this resubmit request.");
        }

        if (resubmitRequest.Status != ResubmitRequestStatuses.Pending)
        {
            throw new InvalidOperationException("Yêu cầu này đã được xử lý trước đó.");
        }

        var extraAttempts = request.ExtraAttempts ?? (request.NewDueDate.HasValue ? null : 1);
        if (extraAttempts is < 0)
        {
            throw new ArgumentException("Số lần nộp thêm không được âm.");
        }

        resubmitRequest.Status = ResubmitRequestStatuses.Approved;
        resubmitRequest.GrantedExtraAttempts = extraAttempts;
        resubmitRequest.GrantedNewDueDate = request.NewDueDate;
        resubmitRequest.ReviewNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        resubmitRequest.ReviewedById = currentUser.Id;
        resubmitRequest.ReviewedAt = _dateTimeProvider.UtcNow;

        _resubmitRequestRepository.Update(resubmitRequest);
        await _resubmitRequestRepository.SaveChangesAsync(cancellationToken);

        await _notificationRepository.AddAsync(new Notification
        {
            UserId = resubmitRequest.StudentId,
            Title = "Yêu cầu nộp lại đã được duyệt",
            Content = $"Giáo viên đã đồng ý cho bạn nộp lại bài \"{resubmitRequest.Assignment?.Title}\".",
            Type = "ResubmitRequestReviewed"
        }, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return ResubmitRequestResponseMapper.Map(resubmitRequest);
    }
}
