using STEM.Application.Dtos.Grading;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class UpdateSubmissionCommentHandler
{
    private readonly ISubmissionCommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;

    public UpdateSubmissionCommentHandler(
        ISubmissionCommentRepository commentRepository,
        IUserRepository userRepository)
    {
        _commentRepository = commentRepository;
        _userRepository = userRepository;
    }

    public async Task<SubmissionCommentResponse> Handle(
        int submissionId,
        int commentId,
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

        var comment = await _commentRepository.GetByIdAsync(commentId, cancellationToken);
        if (comment == null || comment.SubmissionId != submissionId)
        {
            throw new KeyNotFoundException("Comment not found.");
        }

        if (comment.AuthorId != currentUser.Id)
        {
            throw new UnauthorizedAccessException("You are not allowed to edit this comment.");
        }

        comment.Body = body;
        comment.UpdatedAt = DateTime.UtcNow;
        _commentRepository.Update(comment);
        await _commentRepository.SaveChangesAsync(cancellationToken);

        var reloaded = await _commentRepository.GetBySubmissionIdAsync(submissionId, cancellationToken);
        var updated = reloaded.First(item => item.Id == commentId);
        return SubmissionCommentResponseMapper.Map(updated);
    }
}
