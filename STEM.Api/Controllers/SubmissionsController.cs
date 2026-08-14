using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFileRepository _fileRepository;

    public SubmissionsController(
        IAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        IUserRepository userRepository,
        IFileRepository fileRepository)
    {
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
        _fileRepository = fileRepository;
    }

    [HttpPost("text-report")]
    public async Task<IActionResult> SubmitTextReport(
        [FromBody] SubmitTextReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var studentId = GetCurrentUserId();

            var assignment = await _assignmentRepository.GetByIdWithDetailsAsync(request.AssignmentId, cancellationToken);
            if (assignment == null)
                return NotFound(new { success = false, message = "Assignment not found." });

            if (!string.Equals(assignment.AssignmentType, AssignmentTypes.TextReport, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "This assignment is not a text report." });

            if (assignment.Status != AssignmentStatuses.Published)
                return BadRequest(new { success = false, message = "Assignment is not published." });

            if (assignment.DueDate.HasValue && assignment.DueDate.Value < DateTime.UtcNow)
                return BadRequest(new { success = false, message = "Assignment deadline has passed." });

            var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
            if (student == null)
                return Forbid();

            var attemptCount = await _submissionRepository.GetAttemptCountAsync(request.AssignmentId, studentId, cancellationToken);

            if (!assignment.AllowResubmit && attemptCount > 0)
                return BadRequest(new { success = false, message = "Resubmission is not allowed for this assignment." });

            if (assignment.ResubmitLimit.HasValue && attemptCount >= assignment.ResubmitLimit.Value)
                return BadRequest(new { success = false, message = $"You have reached the maximum number of attempts ({assignment.ResubmitLimit.Value})." });

            // Handle file upload if file data is provided
            int? fileId = null;
            string? fileUrl = null;
            
            if (request.FileData != null && !string.IsNullOrEmpty(request.FileData.Url))
            {
                var submissionFile = new SubmissionFile
                {
                    Url = request.FileData.Url,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _fileRepository.AddAsync(submissionFile, cancellationToken);
                await _fileRepository.SaveChangesAsync(cancellationToken);
                fileId = submissionFile.Id;
                fileUrl = request.FileData.Url;
            }

            var contentJson = JsonSerializer.Serialize(new
            {
                content = request.Content,
                fileId = fileId,
                fileUrl = fileUrl,
                fileName = request.FileData?.FileName
            });

            var submission = new Submission
            {
                AssignmentId = request.AssignmentId,
                StudentId = studentId,
                SubmittedAt = DateTime.UtcNow,
                Status = SubmissionStatuses.Submitted,
                ContentJson = contentJson,
                FileId = fileId,
                AttemptNumber = attemptCount + 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _submissionRepository.AddAsync(submission, cancellationToken);
            await _submissionRepository.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                data = new
                {
                    submissionId = submission.Id,
                    attemptNumber = submission.AttemptNumber,
                    status = submission.Status,
                    submittedAt = submission.SubmittedAt
                }
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to submit report.", error = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User is not authenticated.");
    }
}

public class SubmitTextReportRequest
{
    public int AssignmentId { get; set; }
    public string? Content { get; set; }
    public FileDataDto? FileData { get; set; }
}

public class FileDataDto
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
