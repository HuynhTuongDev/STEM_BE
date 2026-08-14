using System.Text.Json;
using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Assignments;

public class SubmitSimulationAssignmentHandler
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;

    public SubmitSimulationAssignmentHandler(
        IAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository)
    {
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
    }

    public async Task<SubmitSimulationResponse> Handle(
        int assignmentId,
        SubmitSimulationRequest request,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _assignmentRepository.GetByIdWithDetailsAsync(assignmentId, cancellationToken);
        if (assignment == null)
            throw new KeyNotFoundException("Assignment not found.");

        if (!string.Equals(assignment.AssignmentType, AssignmentTypes.PracticalSimulation, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("This assignment is not a practical simulation.");

        if (assignment.Status != AssignmentStatuses.Published)
            throw new InvalidOperationException("Assignment is not published.");

        if (assignment.DueDate.HasValue && assignment.DueDate.Value < DateTime.UtcNow)
            throw new InvalidOperationException("Assignment deadline has passed.");

        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
            throw new UnauthorizedAccessException("Student not found.");

        var attemptCount = await _submissionRepository.GetAttemptCountAsync(assignmentId, studentId, cancellationToken);

        if (!assignment.AllowResubmit && attemptCount > 0)
            throw new InvalidOperationException("Resubmission is not allowed for this assignment.");

        if (assignment.ResubmitLimit.HasValue && attemptCount >= assignment.ResubmitLimit.Value)
            throw new InvalidOperationException($"You have reached the maximum number of attempts ({assignment.ResubmitLimit.Value}).");

        var isCorrect = false;
        var validationMessage = "";
        var score = 0m;

        if (assignment.SimulationDetail?.AutoGradingEnabled == true && request.Circuit.ValueKind != JsonValueKind.Undefined)
        {
            var expected = NormalizeJson(assignment.SimulationDetail.AnswerKeyJson);
            var actual = NormalizeJson(request.Circuit);
            isCorrect = expected == actual;

            validationMessage = isCorrect
                ? "Circuit matches the answer key."
                : "Circuit does not match the answer key.";

            if (isCorrect)
            {
                score = assignment.MaxScore;
            }
        }

        var contentJson = JsonSerializer.Serialize(new
        {
            circuit = request.Circuit,
            code = request.Code,
            description = request.Description
        });

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatuses.Submitted,
            ContentJson = contentJson,
            AutoScore = score,
            FinalScore = score,
            Score = score,
            AttemptNumber = attemptCount + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _submissionRepository.AddAsync(submission, cancellationToken);
        await _submissionRepository.SaveChangesAsync(cancellationToken);

        return new SubmitSimulationResponse
        {
            SubmissionId = submission.Id,
            AttemptNumber = submission.AttemptNumber,
            Score = score,
            MaxScore = assignment.MaxScore,
            IsCorrect = isCorrect,
            ValidationMessage = validationMessage,
            IsAutoGraded = assignment.SimulationDetail?.AutoGradingEnabled == true
        };
    }

    private static string NormalizeJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement);
        }
        catch
        {
            return json;
        }
    }

    private static string NormalizeJson(JsonElement element)
    {
        return JsonSerializer.Serialize(element);
    }
}
