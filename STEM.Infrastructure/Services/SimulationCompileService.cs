using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Simulations;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class SimulationCompileService : ISimulationCompileService
{
    private const int MaxCodeLength = 200_000;
    private const string SupportedBoard = "arduino:avr:uno";
    private static readonly Regex LineErrorRegex = new(@"(?:^|\s)(?<line>\d+):\d+:\s+(?<message>.+)$", RegexOptions.Compiled);
    private static readonly ConcurrentDictionary<string, CompileJobResponse> Jobs = new();

    private readonly StemDbContext _context;
    private readonly IConfiguration _configuration;

    public SimulationCompileService(StemDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<CompileSimulationResponse> CompileAsync(
        CompileSimulationRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        Jobs[jobId] = new CompileJobResponse
        {
            JobId = jobId,
            Status = "running",
            CreatedAt = now,
            UpdatedAt = now
        };

        var result = await CompileCoreAsync(request, jobId, cancellationToken);
        Jobs[jobId] = new CompileJobResponse
        {
            JobId = jobId,
            Status = result.Success ? "completed" : "failed",
            Result = result,
            CreatedAt = now,
            UpdatedAt = DateTime.UtcNow
        };

        return result;
    }

    public Task<CompileJobResponse?> GetJobAsync(
        string jobId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        Jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    private async Task<CompileSimulationResponse> CompileCoreAsync(
        CompileSimulationRequest request,
        string jobId,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateRequestAsync(request, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return new CompileSimulationResponse
            {
                Success = false,
                JobId = jobId,
                Errors = validationErrors
            };
        }

        var arduinoCliPath = _configuration["SimulationCompile:ArduinoCliPath"] ?? "arduino-cli";
        var timeoutSeconds = int.TryParse(_configuration["SimulationCompile:TimeoutSeconds"], out var parsedTimeout)
            ? Math.Clamp(parsedTimeout, 1, 60)
            : 15;
        var workingRoot = _configuration["SimulationCompile:WorkingDirectory"];
        if (string.IsNullOrWhiteSpace(workingRoot))
        {
            workingRoot = Path.Combine(Path.GetTempPath(), "stem-simulation-compile");
        }

        var jobRoot = Path.Combine(workingRoot, jobId);
        var sketchDir = Path.Combine(jobRoot, "sketch");
        var outputDir = Path.Combine(jobRoot, "out");

        try
        {
            Directory.CreateDirectory(sketchDir);
            Directory.CreateDirectory(outputDir);
            await File.WriteAllTextAsync(Path.Combine(sketchDir, "sketch.ino"), request.Code, Encoding.UTF8, cancellationToken);

            using var process = new Process();
            process.StartInfo.FileName = arduinoCliPath;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.ArgumentList.Add("compile");
            process.StartInfo.ArgumentList.Add("--fqbn");
            process.StartInfo.ArgumentList.Add(SupportedBoard);
            process.StartInfo.ArgumentList.Add("--output-dir");
            process.StartInfo.ArgumentList.Add(outputDir);
            process.StartInfo.ArgumentList.Add(sketchDir);

            try
            {
                process.Start();
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
            {
                return BuildServiceUnavailable(jobId, arduinoCliPath);
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var exited = await WaitForExitAsync(process, TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);

            if (!exited)
            {
                TryKill(process);
                var timeoutStdout = await stdoutTask;
                var timeoutStderr = await stderrTask;
                var timeoutOutput = string.Join(Environment.NewLine, new[] { timeoutStdout, timeoutStderr }.Where(value => !string.IsNullOrWhiteSpace(value)));
                return new CompileSimulationResponse
                {
                    Success = false,
                    JobId = jobId,
                    CompilerOutput = timeoutOutput,
                    Errors = new[]
                    {
                        new CompileSimulationError { Message = $"Compile timed out after {timeoutSeconds} seconds." }
                    }
                };
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var compilerOutput = string.Join(Environment.NewLine, new[] { stdout, stderr }.Where(value => !string.IsNullOrWhiteSpace(value)));

            if (process.ExitCode != 0)
            {
                return new CompileSimulationResponse
                {
                    Success = false,
                    JobId = jobId,
                    CompilerOutput = compilerOutput,
                    Errors = ParseCompilerErrors(compilerOutput)
                };
            }

            var hexPath = Directory
                .EnumerateFiles(outputDir, "*.hex", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (hexPath == null)
            {
                return new CompileSimulationResponse
                {
                    Success = false,
                    JobId = jobId,
                    CompilerOutput = compilerOutput,
                    Errors = new[]
                    {
                        new CompileSimulationError { Message = "Compile completed but no .hex file was produced." }
                    }
                };
            }

            var hexBytes = await File.ReadAllBytesAsync(hexPath, cancellationToken);
            return new CompileSimulationResponse
            {
                Success = true,
                JobId = jobId,
                HexBase64 = Convert.ToBase64String(hexBytes),
                CompilerOutput = compilerOutput,
                Errors = Array.Empty<CompileSimulationError>()
            };
        }
        finally
        {
            TryDelete(jobRoot);
        }
    }

    private async Task<List<CompileSimulationError>> ValidateRequestAsync(
        CompileSimulationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<CompileSimulationError>();

        if (request.Board != SupportedBoard)
        {
            errors.Add(new CompileSimulationError { Message = "Only arduino:avr:uno is supported." });
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            errors.Add(new CompileSimulationError { Message = "Code is required." });
        }
        else if (request.Code.Length > MaxCodeLength)
        {
            errors.Add(new CompileSimulationError { Message = $"Code is too large. Max length is {MaxCodeLength} characters." });
        }

        if (request.LabId.HasValue)
        {
            var lab = await _context.Labs
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.LabId.Value, cancellationToken);
            if (lab == null)
            {
                errors.Add(new CompileSimulationError { Message = "Lab not found." });
            }
            else if (lab.SimulationMode != LabSimulationModes.CustomSandbox)
            {
                errors.Add(new CompileSimulationError { Message = "Compile is only available for custom_sandbox labs." });
            }
        }

        if (request.AssignmentId.HasValue)
        {
            var assignment = await _context.Assignments
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.AssignmentId.Value, cancellationToken);
            if (assignment == null)
            {
                errors.Add(new CompileSimulationError { Message = "Assignment not found." });
            }
            else if (assignment.AssignmentType != AssignmentTypes.PracticalSimulation)
            {
                errors.Add(new CompileSimulationError { Message = "Compile is only available for practical simulation assignments." });
            }
        }

        return errors;
    }

    private static async Task<bool> WaitForExitAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var waitTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(timeout, cancellationToken);
        return await Task.WhenAny(waitTask, timeoutTask) == waitTask;
    }

    private static CompileSimulationResponse BuildServiceUnavailable(string jobId, string arduinoCliPath)
    {
        return new CompileSimulationResponse
        {
            Success = false,
            JobId = jobId,
            Errors = new[]
            {
                new CompileSimulationError
                {
                    Message = $"Arduino CLI is not configured or not found at '{arduinoCliPath}'."
                }
            }
        };
    }

    private static IReadOnlyCollection<CompileSimulationError> ParseCompilerErrors(string compilerOutput)
    {
        var errors = compilerOutput
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                var match = LineErrorRegex.Match(line);
                return match.Success
                    ? new CompileSimulationError
                    {
                        Line = int.Parse(match.Groups["line"].Value),
                        Message = match.Groups["message"].Value.Trim()
                    }
                    : null;
            })
            .Where(error => error != null)
            .Cast<CompileSimulationError>()
            .ToList();

        return errors.Count > 0
            ? errors
            : new[] { new CompileSimulationError { Message = string.IsNullOrWhiteSpace(compilerOutput) ? "Compile failed." : compilerOutput } };
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }
}
