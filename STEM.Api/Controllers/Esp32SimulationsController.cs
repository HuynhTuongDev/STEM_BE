using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;

namespace STEM.Api.Controllers;

[ApiController]
[Route("api/simulations/esp32")]
public class Esp32SimulationsController : ControllerBase
{
    private readonly IVirtualLabRuntimeService _runtimeService;
    private readonly ISimulationCompileService _compileService;

    public Esp32SimulationsController(
        IVirtualLabRuntimeService runtimeService,
        ISimulationCompileService compileService)
    {
        _runtimeService = runtimeService;
        _compileService = compileService;
    }

    [HttpPost("run")]
    [ProducesResponseType(typeof(RunEsp32SimulationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Run(
        [FromBody] RunEsp32SimulationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _runtimeService.RunEsp32Async(
                request,
                TryGetCurrentUserId(),
                cancellationToken);

            return Ok(response);
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

    [HttpPost("compile")]
    [ProducesResponseType(typeof(Esp32CompileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compile(
        [FromBody] Esp32CompileRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _compileService.CompileAsync(new CompileSimulationRequest
        {
            AssignmentId = request.AssignmentId,
            LabId = request.LabId,
            Board = request.Board,
            Framework = request.Framework,
            SourceCode = request.SourceCode,
            Code = request.SourceCode
        }, TryGetCurrentUserId() ?? 0, cancellationToken);

        return Ok(new Esp32CompileResponse
        {
            Success = result.Success,
            JobId = result.JobId,
            Logs = SplitLogs(result.CompilerOutput),
            Firmware = result.Success
                ? new Esp32FirmwareResponse
                {
                    FileName = result.FirmwareFileName,
                    Format = result.FirmwareFormat,
                    Base64 = result.FirmwareBase64 ?? result.HexBase64
                }
                : null,
            Errors = result.Errors
        });
    }

    private int? TryGetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static IReadOnlyCollection<string> SplitLogs(string? compilerOutput)
    {
        return string.IsNullOrWhiteSpace(compilerOutput)
            ? Array.Empty<string>()
            : compilerOutput
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToArray();
    }
}
