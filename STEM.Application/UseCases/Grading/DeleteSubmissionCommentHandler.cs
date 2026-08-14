using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class DeleteSubmissionCommentHandler
{
    private readonly ISubmissionCommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;

    public DeleteSubmissionCommentHandler(
        ISubmissionCommentRepository commentRepository,
        IUserRepository userRepository)
    {
        _commentRepository = commentRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(
        int submissionId,
        int commentId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
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
            throw new UnauthorizedAccessException("You are not allowed to delete this comment.");
        }

        _commentRepository.Delete(comment);
        await _commentRepository.SaveChangesAsync(cancellationToken);
    }
}
