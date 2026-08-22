using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.UseCases.Syllabuses;

namespace STEM.Api.Controllers;

/// <summary>
/// API danh mục khối lớp (reference data cho Standard Syllabus/Class).
/// </summary>
[ApiController]
[Route("api/grade-levels")]
[Authorize]
public class GradeLevelsController : ControllerBase
{
    private readonly GetGradeLevelsListHandler _getGradeLevelsListHandler;

    public GradeLevelsController(GetGradeLevelsListHandler getGradeLevelsListHandler)
    {
        _getGradeLevelsListHandler = getGradeLevelsListHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetGradeLevels(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getGradeLevelsListHandler.Handle(cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching grade levels.", error = ex.Message });
        }
    }
}
