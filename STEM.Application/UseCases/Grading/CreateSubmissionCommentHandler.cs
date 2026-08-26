using STEM.Application.Dtos.Grading;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class CreateSubmissionCommentHandler
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ISubmissionCommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;

    public CreateSubmissionCommentHandler(
        ISubmissionRepository submissionRepository,
        ISubmissionCommentRepository commentRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository)
    {
        _submissionRepository = submissionRepository;
        _commentRepository = commentRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<SubmissionCommentResponse> Handle(
        int submissionId,
        SubmissionCommentRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var body = request.Body?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Nội dung bình luận không được để trống.");
        }

        if (body.Length > 2000)
        {
            throw new ArgumentException("Nội dung bình luận không được vượt quá 2000 ký tự.");
        }

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

        if (!SubmissionAuthorization.CanCommentOnSubmission(currentUser, submission))
        {
            throw new UnauthorizedAccessException("You are not allowed to comment on this submission.");
        }

        var comment = new SubmissionComment
        {
            SubmissionId = submissionId,
            AuthorId = currentUser.Id,
            Body = body
        };

        await _commentRepository.AddAsync(comment, cancellationToken);
        await _commentRepository.SaveChangesAsync(cancellationToken);
        comment.Author = currentUser;

        await NotifyOtherPartyAsync(currentUser, submission, cancellationToken);

        return SubmissionCommentResponseMapper.Map(comment);
    }

    private async Task NotifyOtherPartyAsync(
        User author,
        Submission submission,
        CancellationToken cancellationToken)
    {
        var classEntity = submission.Assignment?.Class;
        if (classEntity == null || submission.StudentId == null)
        {
            return;
        }

        var isTeacherAuthor = author.Role?.Name == RoleNames.Teacher;
        var recipientId = isTeacherAuthor ? submission.StudentId.Value : classEntity.TeacherId;

        if (recipientId == author.Id)
        {
            return;
        }

        await _notificationRepository.AddAsync(new Notification
        {
            UserId = recipientId,
            Title = "Bình luận mới",
            Content = $"{author.FullName} đã bình luận trên bài nộp \"{submission.Assignment!.Title}\".",
            Type = NotificationType.SubmissionComment
        }, cancellationToken);
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }
}
