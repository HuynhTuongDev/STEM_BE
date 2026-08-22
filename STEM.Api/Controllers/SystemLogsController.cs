using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.SystemLogs;
using STEM.Application.UseCases.SystemLogs;

namespace STEM.Api.Controllers;

/// <summary>
/// Read-only view of the System Audit Log. Master Administrator only — this
/// is a business/security audit trail, not application diagnostics.
/// </summary>
[ApiController]
[Route("api/system-logs")]
[Authorize(Policy = "MasterOnly")]
public class SystemLogsController : ControllerBase
{
    private readonly GetSystemLogsListHandler _getSystemLogsListHandler;

    public SystemLogsController(GetSystemLogsListHandler getSystemLogsListHandler)
    {
        _getSystemLogsListHandler = getSystemLogsListHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetSystemLogs(
        [FromQuery] GetSystemLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getSystemLogsListHandler.Handle(request, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching system logs.", error = ex.Message });
        }
    }
}
