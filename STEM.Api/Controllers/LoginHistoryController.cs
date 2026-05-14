using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.LoginHistory;
using STEM.Application.UseCases.LoginHistory;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginHistoryController : ControllerBase
{
    private readonly GetLoginHistoriesHandler _getLoginHistoriesHandler;

    public LoginHistoryController(GetLoginHistoriesHandler getLoginHistoriesHandler)
    {
        _getLoginHistoriesHandler = getLoginHistoriesHandler;
    }

    /// <summary>
    /// Get login history for a specific user
    /// </summary>
    /// <param name="request">Request containing UserId and pagination</param>
    /// <returns>List of login history records with timestamps</returns>
    [HttpPost("get-histories")]
    [ProducesResponseType(typeof(List<LoginHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLoginHistories(
        [FromBody] GetLoginHistoriesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var histories = await _getLoginHistoriesHandler.Handle(request, cancellationToken);
            return Ok(new { total = histories.Count, data = histories });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
