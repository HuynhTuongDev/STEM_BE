using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Curriculum;
using STEM.Application.UseCases.Curriculum;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GradeLevelsController : ControllerBase
{
    private readonly GetGradeLevelsHandler _getGradeLevelsHandler;
    private readonly GetGradeLevelByIdHandler _getGradeLevelByIdHandler;
    private readonly CreateGradeLevelHandler _createGradeLevelHandler;
    private readonly UpdateGradeLevelHandler _updateGradeLevelHandler;
    private readonly DeleteGradeLevelHandler _deleteGradeLevelHandler;

    public GradeLevelsController(
        GetGradeLevelsHandler getGradeLevelsHandler,
        GetGradeLevelByIdHandler getGradeLevelByIdHandler,
        CreateGradeLevelHandler createGradeLevelHandler,
        UpdateGradeLevelHandler updateGradeLevelHandler,
        DeleteGradeLevelHandler deleteGradeLevelHandler)
    {
        _getGradeLevelsHandler = getGradeLevelsHandler;
        _getGradeLevelByIdHandler = getGradeLevelByIdHandler;
        _createGradeLevelHandler = createGradeLevelHandler;
        _updateGradeLevelHandler = updateGradeLevelHandler;
        _deleteGradeLevelHandler = deleteGradeLevelHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getGradeLevelsHandler.Handle(cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getGradeLevelByIdHandler.Handle(id, cancellationToken);
            if (result == null)
                return NotFound(new { success = false, message = "Grade level not found." });

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Create(
        [FromBody] CreateGradeLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _createGradeLevelHandler.Handle(request, cancellationToken);
            return Ok(new { success = true, data = new { id } });
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
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateGradeLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _updateGradeLevelHandler.Handle(id, request, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Grade level not found." });

            return Ok(new { success = true, message = "Grade level updated successfully." });
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
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _deleteGradeLevelHandler.Handle(id, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Grade level not found." });

            return Ok(new { success = true, message = "Grade level deleted successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
