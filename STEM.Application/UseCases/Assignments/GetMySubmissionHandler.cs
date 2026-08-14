using STEM.Application.Dtos.Assignments;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class GetMySubmissionHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;

    public GetMySubmissionHandler(
        IAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
    }

    public async Task<GetMySubmissionResponse?> Handle(
        int assignmentId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _assignmentRepository.GetByIdWithDetailsAsync(assignmentId, cancellationToken);
        if (assignment == null)
            throw new KeyNotFoundException("Assignment not found.");

        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
            throw new UnauthorizedAccessException("Student not found.");

        var submission = await _submissionRepository.GetByAssignmentAndStudentAsync(assignmentId, studentId, cancellationToken);
        if (submission == null)
            return null;

        var attemptCount = await _submissionRepository.GetAttemptCountAsync(assignmentId, studentId, cancellationToken);

        return new GetMySubmissionResponse
        {
            SubmissionId = submission.Id,
            AssignmentId = assignmentId,
            AssignmentTitle = assignment.Title,
            AssignmentType = assignment.AssignmentType,
            Status = submission.Status,
            AttemptNumber = submission.AttemptNumber,
            Score = submission.Score,
            MaxScore = assignment.MaxScore,
            ContentJson = submission.ContentJson,
            FileId = submission.FileId,
            FileUrl = submission.File?.Url,
            Feedback = submission.Feedback,
            GradedAt = submission.GradedAt,
            SubmittedAt = submission.SubmittedAt ?? submission.CreatedAt,
            CanResubmit = assignment.AllowResubmit && (assignment.ResubmitLimit == null || attemptCount < assignment.ResubmitLimit.Value),
            RemainingAttempts = assignment.ResubmitLimit.HasValue
                ? Math.Max(0, assignment.ResubmitLimit.Value - attemptCount)
                : null
        };
    }
}

public class GetMySubmissionsHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;

    public GetMySubmissionsHandler(
        IAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedMySubmissionsResponse> Handle(
        int studentId,
        int pageNumber,
        int pageSize,
        int? assignmentId,
        CancellationToken cancellationToken = default)
    {
        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
            throw new UnauthorizedAccessException("Student not found.");

        var (submissions, totalCount) = await _submissionRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            assignmentId,
            null,
            studentId,
            null,
            null,
            cancellationToken);

        var items = new List<GetMySubmissionsResponse>();
        foreach (var submission in submissions)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId, cancellationToken);
            items.Add(new GetMySubmissionsResponse
            {
                SubmissionId = submission.Id,
                AssignmentId = submission.AssignmentId,
                AssignmentTitle = assignment?.Title ?? "Unknown",
                AssignmentType = assignment?.AssignmentType ?? "",
                Status = submission.Status,
                AttemptNumber = submission.AttemptNumber,
                Score = submission.Score,
                MaxScore = assignment?.MaxScore ?? 100,
                SubmittedAt = submission.SubmittedAt ?? submission.CreatedAt
            });
        }

        return new PagedMySubmissionsResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }
}
