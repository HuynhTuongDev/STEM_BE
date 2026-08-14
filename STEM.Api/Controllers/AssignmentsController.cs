using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Assignments;
using STEM.Application.UseCases.Assignments;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly CreateAssignmentHandler _createAssignmentHandler;
    private readonly GetAssignmentsHandler _getAssignmentsHandler;
    private readonly GetAssignmentDetailHandler _getAssignmentDetailHandler;
    private readonly UpdateAssignmentHandler _updateAssignmentHandler;
    private readonly DeleteAssignmentHandler _deleteAssignmentHandler;
    private readonly SubmitQuizAssignmentHandler _submitQuizAssignmentHandler;
    private readonly SubmitReportAssignmentHandler _submitReportAssignmentHandler;
    private readonly SubmitSimulationAssignmentHandler _submitSimulationAssignmentHandler;
    private readonly GetMySubmissionHandler _getMySubmissionHandler;
    private readonly GetMySubmissionsHandler _getMySubmissionsHandler;

    public AssignmentsController(
        CreateAssignmentHandler createAssignmentHandler,
        GetAssignmentsHandler getAssignmentsHandler,
        GetAssignmentDetailHandler getAssignmentDetailHandler,
        UpdateAssignmentHandler updateAssignmentHandler,
        DeleteAssignmentHandler deleteAssignmentHandler,
        SubmitQuizAssignmentHandler submitQuizAssignmentHandler,
        SubmitReportAssignmentHandler submitReportAssignmentHandler,
        SubmitSimulationAssignmentHandler submitSimulationAssignmentHandler,
        GetMySubmissionHandler getMySubmissionHandler,
        GetMySubmissionsHandler getMySubmissionsHandler)
    {
        _createAssignmentHandler = createAssignmentHandler;
        _getAssignmentsHandler = getAssignmentsHandler;
        _getAssignmentDetailHandler = getAssignmentDetailHandler;
        _updateAssignmentHandler = updateAssignmentHandler;
        _deleteAssignmentHandler = deleteAssignmentHandler;
        _submitQuizAssignmentHandler = submitQuizAssignmentHandler;
        _submitReportAssignmentHandler = submitReportAssignmentHandler;
        _submitSimulationAssignmentHandler = submitSimulationAssignmentHandler;
        _getMySubmissionHandler = getMySubmissionHandler;
        _getMySubmissionsHandler = getMySubmissionsHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] GetAssignmentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getAssignmentsHandler.Handle(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get assignments.", error = ex.Message });
        }
    }

    [HttpGet("student")]
    public async Task<IActionResult> GetStudentAssignments(
        [FromQuery] GetAssignmentsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Override StudentId với current user
            request.StudentId = GetCurrentUserId();
            var response = await _getAssignmentsHandler.Handle(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get student assignments.", error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAssignment(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getAssignmentDetailHandler.Handle(id, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get assignment.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAssignment(
        [FromBody] CreateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _createAssignmentHandler.Handle(request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to create assignment.", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAssignment(
        int id,
        [FromBody] UpdateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _updateAssignmentHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to update assignment.", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAssignment(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _deleteAssignmentHandler.Handle(id, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, message = "Assignment deleted successfully." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to delete assignment.", error = ex.Message });
        }
    }

    [HttpGet("{id:int}/simulation/base-diagram")]
    public async Task<IActionResult> GetSimulationBaseDiagram(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getAssignmentDetailHandler.Handle(id, GetCurrentUserId(), cancellationToken);
            if (!string.Equals(response.AssignmentType, "practical_simulation", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Assignment is not a practical simulation." });

            if (response.SimulationDetail == null)
                return NotFound(new { success = false, message = "Simulation detail not found." });

            return Ok(new { success = true, data = response.SimulationDetail.BaseDiagram });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get simulation base diagram.", error = ex.Message });
        }
    }

    [HttpPost("{id:int}/simulation/validate")]
    public async Task<IActionResult> ValidateSimulationCircuit(
        int id,
        [FromBody] SimulationValidateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Circuit.ValueKind == JsonValueKind.Undefined)
                return BadRequest(new { success = false, message = "Circuit is required." });

            var response = await _getAssignmentDetailHandler.Handle(id, GetCurrentUserId(), cancellationToken);
            if (!string.Equals(response.AssignmentType, "practical_simulation", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Assignment is not a practical simulation." });

            if (response.SimulationDetail == null)
                return NotFound(new { success = false, message = "Simulation detail not found." });

            var expected = NormalizeJson(response.SimulationDetail.AnswerKey);
            var actual = NormalizeJson(request.Circuit);
            var isValid = expected == actual;

            return Ok(new
            {
                success = true,
                data = new SimulationValidateResponse
                {
                    IsValid = isValid,
                    Message = isValid ? "Circuit matches the answer key." : "Circuit does not match the answer key."
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to validate simulation circuit.", error = ex.Message });
        }
    }

    [HttpPost("{id:int}/submit-quiz")]
    public async Task<IActionResult> SubmitQuiz(
        int id,
        [FromBody] SubmitQuizRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _submitQuizAssignmentHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
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
            var innerException = ex.InnerException?.Message ?? ex.Message;
            var stackTrace = ex.StackTrace;
            Console.WriteLine($"[ERROR] SubmitQuiz failed: {ex.Message}");
            Console.WriteLine($"[ERROR] Inner Exception: {innerException}");
            Console.WriteLine($"[ERROR] Stack Trace: {stackTrace}");
            return StatusCode(500, new { success = false, message = "Failed to submit quiz.", error = innerException, stackTrace = stackTrace });
        }
    }

    [HttpPost("{id:int}/submit-report")]
    public async Task<IActionResult> SubmitReport(
        int id,
        [FromBody] SubmitReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _submitReportAssignmentHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
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

    [HttpPost("{id:int}/submit-simulation")]
    public async Task<IActionResult> SubmitSimulation(
        int id,
        [FromBody] SubmitSimulationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _submitSimulationAssignmentHandler.Handle(id, request, GetCurrentUserId(), cancellationToken);
            return Ok(new { success = true, data = response });
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
            return StatusCode(500, new { success = false, message = "Failed to submit simulation.", error = ex.Message });
        }
    }

    [HttpGet("{id:int}/my-submission")]
    public async Task<IActionResult> GetMySubmission(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getMySubmissionHandler.Handle(id, GetCurrentUserId(), cancellationToken);
            if (response == null)
                return NotFound(new { success = false, message = "You have not submitted this assignment yet." });

            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get submission.", error = ex.Message });
        }
    }

    [HttpGet("my-submissions")]
    public async Task<IActionResult> GetMySubmissions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? assignmentId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _getMySubmissionsHandler.Handle(GetCurrentUserId(), pageNumber, pageSize, assignmentId, cancellationToken);
            return Ok(new { success = true, data = response });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Failed to get submissions.", error = ex.Message });
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

    private static string NormalizeJson(JsonElement value)
    {
        return JsonSerializer.Serialize(value);
    }
}
