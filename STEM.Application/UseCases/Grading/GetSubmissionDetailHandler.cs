using STEM.Application.Dtos.Grading;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class GetSubmissionDetailHandler
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;

    public GetSubmissionDetailHandler(
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository)
    {
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
    }

    public async Task<SubmissionResponse> Handle(
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
            throw new UnauthorizedAccessException("You are not allowed to view this submission.");
        }

        return SubmissionResponseMapper.Map(submission);
    }
}
