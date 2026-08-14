using STEM.Application.Dtos.Grading;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class GetSubmissionCommentsHandler
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ISubmissionCommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;

    public GetSubmissionCommentsHandler(
        ISubmissionRepository submissionRepository,
        ISubmissionCommentRepository commentRepository,
        IUserRepository userRepository)
    {
        _submissionRepository = submissionRepository;
        _commentRepository = commentRepository;
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyCollection<SubmissionCommentResponse>> Handle(
        int submissionId,
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

        if (!SubmissionAuthorization.CanViewSubmission(currentUser, submission))
        {
            throw new UnauthorizedAccessException("You are not allowed to view this submission's discussion.");
        }

        var comments = await _commentRepository.GetBySubmissionIdAsync(submissionId, cancellationToken);
        return comments.Select(SubmissionCommentResponseMapper.Map).ToList();
    }
}
