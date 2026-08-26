using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Curriculum;
using STEM.Application.UseCases.Curriculum;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModulesController : ControllerBase
{
    private readonly GetModulesHandler _getModulesHandler;
    private readonly GetModuleByIdHandler _getModuleByIdHandler;
    private readonly CreateModuleHandler _createModuleHandler;
    private readonly UpdateModuleHandler _updateModuleHandler;
    private readonly DeleteModuleHandler _deleteModuleHandler;
    private readonly ReorderModulesHandler _reorderModulesHandler;
    private readonly GetModulesByClassHandler _getModulesByClassHandler;

    public ModulesController(
        GetModulesHandler getModulesHandler,
        GetModuleByIdHandler getModuleByIdHandler,
        CreateModuleHandler createModuleHandler,
        UpdateModuleHandler updateModuleHandler,
        DeleteModuleHandler deleteModuleHandler,
        ReorderModulesHandler reorderModulesHandler,
        GetModulesByClassHandler getModulesByClassHandler)
    {
        _getModulesHandler = getModulesHandler;
        _getModuleByIdHandler = getModuleByIdHandler;
        _createModuleHandler = createModuleHandler;
        _updateModuleHandler = updateModuleHandler;
        _deleteModuleHandler = deleteModuleHandler;
        _reorderModulesHandler = reorderModulesHandler;
        _getModulesByClassHandler = getModulesByClassHandler;
    }

    [HttpGet("by-course/{courseId}")]
    public async Task<IActionResult> GetByCourse(int courseId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getModulesHandler.Handle(courseId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("by-class/{classId}")]
    [Authorize(Policy = "TeacherAndAbove")]
    public async Task<IActionResult> GetByClass(int classId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _getModulesByClassHandler.Handle(classId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
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
            var result = await _getModuleByIdHandler.Handle(id, cancellationToken);
            if (result == null)
                return NotFound(new { success = false, message = "Module not found." });

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
        [FromBody] CreateModuleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _createModuleHandler.Handle(request, cancellationToken);
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
        [FromBody] UpdateModuleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _updateModuleHandler.Handle(id, request, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Module not found." });

            return Ok(new { success = true, message = "Module updated successfully." });
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
            var success = await _deleteModuleHandler.Handle(id, cancellationToken);
            if (!success)
                return NotFound(new { success = false, message = "Module not found." });

            return Ok(new { success = true, message = "Module deleted successfully." });
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

    [HttpPost("reorder")]
    [Authorize(Policy = "MasterAdminOnly")]
    public async Task<IActionResult> Reorder(
        [FromBody] ReorderModulesRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _reorderModulesHandler.Handle(request, cancellationToken);
            return Ok(new { success = true, message = "Modules reordered successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
