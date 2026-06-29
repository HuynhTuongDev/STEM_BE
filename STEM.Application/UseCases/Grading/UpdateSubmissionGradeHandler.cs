using STEM.Application.Dtos.Grading;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Grading;

public class UpdateSubmissionGradeHandler
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;

    public UpdateSubmissionGradeHandler(
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository)
    {
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
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

        return SubmissionResponseMapper.Map(submission);
    }
}
