using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Dtos.VirtualLab;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Users;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/virtual-lab/projects")]
[Authorize]
public class VirtualLabProjectController : ControllerBase
{
    private readonly IVirtualLabProjectService _service;
    private readonly IVirtualLabRuntimeService _runtimeService;

    public VirtualLabProjectController(
        IVirtualLabProjectService service,
        IVirtualLabRuntimeService runtimeService)
    {
        _service = service;
        _runtimeService = runtimeService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] VirtualLabProjectRequest request)
    {
        try
        {
            var project = await _service.CreateProjectAsync(request, GetCurrentUserId());
            return Ok(project);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        try
        {
            var project = await _service.GetProjectAsync(id, GetCurrentUserId());
            if (project == null) return NotFound();

            return Ok(project);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] VirtualLabProjectRequest request)
    {
        try
        {
            var project = await _service.UpdateProjectAsync(id, request, GetCurrentUserId());
            if (project == null) return NotFound();

            return Ok(project);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartSimulation(
        Guid id,
        [FromBody] StartSimulationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _runtimeService.RunEsp32Async(new RunEsp32SimulationRequest
            {
                SessionId = id.ToString("N"),
                LabId = request.LabId,
                DiagramJson = ToDiagramJson(request.Diagram),
                SourceCode = request.Code
            }, GetCurrentUserId(), cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Học sinh đang gõ code (FE debounce 2-3s sau khi ngừng gõ) — kích hoạt
    // compile nền để làm ấm firmware cache TRƯỚC khi bấm Run, không phải lúc
    // Run mới bắt đầu compile. Trả về ngay (202), không đợi compile ~40-90s
    // thật xong — xem TriggerPrecompileAsync/IPrecompileTriggerService.
    [HttpPost("{id}/precompile")]
    public async Task<IActionResult> Precompile(
        Guid id,
        [FromBody] PrecompileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _runtimeService.TriggerPrecompileAsync(id.ToString("N"), request.Code, GetCurrentUserId(), cancellationToken);
            return Accepted();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Snapshot 1 lần (không realtime) cho Teacher "Xem live" — bổ sung phần
    // SignalR-only còn thiếu: học sinh đã join TRƯỚC khi giáo viên mở màn
    // hình thì code/diagram/status hiện tại không tới qua event nào (Hub chỉ
    // phát cho THAY ĐỔI, không phải state hiện tại lúc join) — StudentSandboxViewer.tsx
    // gọi 1 lần lúc mount, sau đó vẫn nhận tiếp qua SignalR như bình thường.
    [HttpGet("{id}/teacher-view")]
    [Authorize(Roles = RoleNames.Teacher)]
    public async Task<IActionResult> GetTeacherView(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await _runtimeService.GetProjectSnapshotForTeacherAsync(
                id.ToString("N"), GetCurrentUserId(), cancellationToken);
            if (snapshot == null) return NotFound();

            return Ok(snapshot);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id}/stop")]
    public async Task<IActionResult> StopSimulation(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopped = await _runtimeService.StopSimulationAsync(id, GetCurrentUserId(), cancellationToken);
            if (!stopped) return NotFound();

            return Ok(new
            {
                sessionId = id.ToString("N"),
                status = VirtualLabProjectStatuses.Stopped,
                events = Array.Empty<SimulationEventResponse>()
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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

    private static string ToDiagramJson(JsonElement diagram)
    {
        if (diagram.ValueKind == JsonValueKind.String)
        {
            return diagram.GetString() ?? "{}";
        }

        return diagram.ValueKind == JsonValueKind.Undefined
            ? "{}"
            : diagram.GetRawText();
    }
}
