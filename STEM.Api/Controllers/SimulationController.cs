using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;

namespace STEM.Api.Controllers;

/// <summary>
/// Controller cho module Wokwi Virtual Lab của StemFlow.
/// Quản lý template, session và gợi ý AI cho thí nghiệm điện tử ảo.
/// </summary>
[ApiController]
[Route("api/simulations")]
public class SimulationController : ControllerBase
{
    private readonly SimulationHandler _simulationHandler;
    private readonly AiSuggestHandler  _aiSuggestHandler;

    public SimulationController(
        SimulationHandler simulationHandler,
        AiSuggestHandler  aiSuggestHandler)
    {
        _simulationHandler = simulationHandler;
        _aiSuggestHandler  = aiSuggestHandler;
    }

    // ── Component Catalog ─────────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách tất cả component được hỗ trợ trong sandbox Wokwi.
    /// </summary>
    /// <remarks>GET /api/simulations/components</remarks>
    [HttpGet("components")]
    [ProducesResponseType(typeof(IEnumerable<ComponentMetaResponse>), StatusCodes.Status200OK)]
    public IActionResult GetComponents()
    {
        var components = _simulationHandler.GetComponents();
        return Ok(components);
    }

    // ── Template ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo simulation template mới từ diagram JSON.
    /// Validate diagram trước khi lưu; trả về 400 nếu không hợp lệ.
    /// </summary>
    /// <remarks>POST /api/simulations/templates</remarks>
    [HttpPost("templates")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _simulationHandler.CreateTemplateAsync(request, cancellationToken);
            return CreatedAtAction(
                nameof(GetTemplate),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi khi tạo template.", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một template theo Id.
    /// </summary>
    /// <remarks>GET /api/simulations/templates/{id}</remarks>
    [HttpGet("templates/{id:int}")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _simulationHandler.GetTemplateAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi khi lấy template.", error = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật diagram JSON của template đã có.
    /// Validate diagram mới trước khi ghi đè.
    /// </summary>
    /// <remarks>PATCH /api/simulations/templates/{id}/diagram</remarks>
    [HttpPatch("templates/{id:int}/diagram")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDiagram(
        int id,
        [FromBody] UpdateDiagramRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _simulationHandler.UpdateDiagramAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi khi cập nhật diagram.", error = ex.Message });
        }
    }

    /// <summary>
    /// Sinh Python code mẫu từ diagram của template.
    /// Code mock RPi.GPIO và Adafruit_DHT, chạy trong Pyodide.
    /// </summary>
    /// <remarks>GET /api/simulations/templates/{id}/python</remarks>
    [HttpGet("templates/{id:int}/python")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPythonCode(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var code = await _simulationHandler.GetPythonCodeAsync(id, cancellationToken);
            return Ok(new { templateId = id, pythonCode = code });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi khi sinh Python code.", error = ex.Message });
        }
    }

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo phiên lab mới cho một student trên một template cụ thể.
    /// </summary>
    /// <remarks>POST /api/simulations/sessions</remarks>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _simulationHandler.CreateSessionAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi khi tạo session.", error = ex.Message });
        }
    }

    // ── AI Suggest ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi Claude AI để phân tích prompt và đề xuất thí nghiệm:
    /// tên linh kiện, diagram JSON và Python code mẫu.
    /// </summary>
    /// <remarks>POST /api/simulations/ai/suggest</remarks>
    [HttpPost("ai/suggest")]
    [ProducesResponseType(typeof(AiSuggestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AiSuggest(
        [FromBody] AiSuggestRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _aiSuggestHandler.Handle(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Lỗi khi gọi AI suggest.", error = ex.Message });
        }
    }
}
