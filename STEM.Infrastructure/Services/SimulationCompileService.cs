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
    private static readonly IReadOnlyDictionary<string, string> SupportedBoards =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["arduino:avr:uno"] = "arduino:avr:uno",
            ["arduino_uno"] = "arduino:avr:uno",
            ["uno"] = "arduino:avr:uno",
            ["esp32"] = "esp32:esp32:esp32",
            ["esp32dev"] = "esp32:esp32:esp32",
            ["esp32:esp32:esp32"] = "esp32:esp32:esp32",
            ["esp32:esp32:esp32dev"] = "esp32:esp32:esp32dev"
        };
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

        var dockerCliPath = _configuration["SimulationCompile:DockerCliPath"] ?? "docker";
        var dockerImage = _configuration["SimulationCompile:DockerImage"] ?? "stem-arduino-cli-sandbox:latest";
        // 1536m/90s are measured, not guessed: a cold compile of a trivial Blink
        // sketch (--cpus 1.0, fresh tmpfs, no cross-request build cache) genuinely
        // peaked near 945MB RSS and took 39s wall-clock. The old 512m/60s-clamped
        // defaults OOM-killed and would timeout-kill that same legitimate compile.
        var memoryLimit = _configuration["SimulationCompile:MemoryLimit"] ?? "1536m";
        var cpuLimit = _configuration["SimulationCompile:CpuLimit"] ?? "1.0";
        var pidsLimit = _configuration["SimulationCompile:PidsLimit"] ?? "128";
        var buildTmpfsSize = _configuration["SimulationCompile:BuildTmpfsSizeMb"] ?? "256";
        var timeoutSeconds = int.TryParse(_configuration["SimulationCompile:TimeoutSeconds"], out var parsedTimeout)
            ? Math.Clamp(parsedTimeout, 1, 120)
            : 90;
        var workingRoot = _configuration["SimulationCompile:WorkingDirectory"];
        if (string.IsNullOrWhiteSpace(workingRoot))
        {
            workingRoot = Path.Combine(Path.GetTempPath(), "stem-simulation-compile");
        }

        var jobRoot = Path.Combine(workingRoot, jobId);
        var sketchDir = Path.Combine(jobRoot, "sketch");
        var outputDir = Path.Combine(jobRoot, "out");
        var containerName = $"stem-compile-{jobId}";

        try
        {
            Directory.CreateDirectory(sketchDir);
            Directory.CreateDirectory(outputDir);
            var sourceCode = GetSourceCode(request);
            var boardFqbn = NormalizeBoard(request.Board);
            // Encoding.UTF8 (the static property) emits a BOM by default, which gcc
            // rejects as a stray token before the first real line ("'U0000feffvoid'
            // does not name a type") — confirmed via a real compile through the API.
            // UTF8Encoding(false) writes UTF-8 without the BOM preamble.
            await File.WriteAllTextAsync(Path.Combine(sketchDir, "sketch.ino"), sourceCode, new UTF8Encoding(false), cancellationToken);

            // Sandboxing: the only host filesystem the container ever sees is this
            // job's own temp dir (sketch input read-only, output read-write), and the
            // whole dir is deleted in the `finally` below regardless of outcome — both
            // are within the explicitly-allowed "own temp working directory" exception.
            // Build *scratch* (intermediate object files, never needs retrieval) lives
            // on a size-capped tmpfs inside the disposable container and never touches
            // host disk. Output is a host bind mount rather than tmpfs: tmpfs contents
            // are torn down the instant the container exits, so `docker cp` afterward
            // cannot see them ("Could not find the file ... in container" — confirmed
            // via direct repro) — bind-mounting output sidesteps that race entirely.
            // `--network none` cuts all network access for the container.
            // `--memory`/`--cpus`/`--pids-limit` are hard kernel-enforced limits (not
            // best-effort), and `--read-only` plus `--cap-drop ALL` mean nothing outside
            // the tmpfs/output mounts is writable even if a limit is missed.
            using var process = new Process();
            process.StartInfo.FileName = dockerCliPath;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            var args = process.StartInfo.ArgumentList;
            args.Add("run");
            args.Add("--name"); args.Add(containerName);
            args.Add("--network"); args.Add("none");
            args.Add("--memory"); args.Add(memoryLimit);
            args.Add("--memory-swap"); args.Add(memoryLimit);
            args.Add("--cpus"); args.Add(cpuLimit);
            args.Add("--pids-limit"); args.Add(pidsLimit);
            args.Add("--cap-drop"); args.Add("ALL");
            args.Add("--security-opt"); args.Add("no-new-privileges");
            args.Add("--read-only");
            args.Add("--user"); args.Add("10001:10001");
            args.Add("-v"); args.Add($"{sketchDir}:/workspace/sketch:ro");
            args.Add("-v"); args.Add($"{outputDir}:/workspace/output:rw");
            args.Add("--tmpfs"); args.Add($"/workspace/build:rw,size={buildTmpfsSize}m,mode=1777");
            // exec is required here: arduino-cli shells out to PyInstaller-packaged
            // tools (esptool et al.) that self-extract into $TMPDIR at runtime and
            // dlopen a bundled libpython .so from there — Docker's --tmpfs defaults
            // to noexec, which makes that dlopen fail (confirmed via direct repro:
            // "failed to map segment from shared object"). Measured real extracted
            // size for esptool is ~10MB; 128m leaves headroom for multiple/concurrent
            // PyInstaller tool invocations in one compile.
            args.Add("--tmpfs"); args.Add("/tmp:rw,exec,size=128m,mode=1777");
            args.Add(dockerImage);
            args.Add("compile");
            args.Add("--fqbn"); args.Add(boardFqbn);
            args.Add("--build-path"); args.Add("/workspace/build/tmp");
            args.Add("--output-dir"); args.Add("/workspace/output");
            args.Add("/workspace/sketch");

            try
            {
                process.Start();
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
            {
                return BuildServiceUnavailable(jobId, dockerCliPath);
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var exited = await WaitForExitAsync(process, TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);

            if (!exited)
            {
                // Second line of defense: killing the `docker run` client process does
                // NOT stop the container in the daemon, so the container must be
                // killed explicitly or the runaway build keeps consuming its
                // (resource-limited, but still real) container until GC in `finally`.
                await TryDockerKillAsync(dockerCliPath, containerName);
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

            // Output lands directly in outputDir via the bind mount above — no
            // docker cp step needed (and none would work reliably after exit; see
            // the mount-setup comment above).
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

            var firmwarePath = Directory
                .EnumerateFiles(outputDir, "*.*", SearchOption.AllDirectories)
                .Where(path =>
                {
                    var extension = Path.GetExtension(path);
                    return extension.Equals(".bin", StringComparison.OrdinalIgnoreCase) ||
                           extension.Equals(".hex", StringComparison.OrdinalIgnoreCase) ||
                           extension.Equals(".elf", StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(path => FirmwareSortOrder(Path.GetExtension(path)))
                .ThenByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (firmwarePath == null)
            {
                return new CompileSimulationResponse
                {
                    Success = false,
                    JobId = jobId,
                    CompilerOutput = compilerOutput,
                    Errors = new[]
                    {
                        new CompileSimulationError { Message = "Compile completed but no firmware artifact (.bin/.hex/.elf) was produced." }
                    }
                };
            }

            var firmwareBytes = await File.ReadAllBytesAsync(firmwarePath, cancellationToken);
            var firmwareBase64 = Convert.ToBase64String(firmwareBytes);
            var firmwareFormat = Path.GetExtension(firmwarePath).TrimStart('.').ToLowerInvariant();
            return new CompileSimulationResponse
            {
                Success = true,
                JobId = jobId,
                HexBase64 = firmwareFormat == "hex" ? firmwareBase64 : null,
                FirmwareBase64 = firmwareBase64,
                FirmwareFileName = Path.GetFileName(firmwarePath),
                FirmwareFormat = firmwareFormat,
                CompilerOutput = compilerOutput,
                Errors = Array.Empty<CompileSimulationError>()
            };
        }
        finally
        {
            await TryDockerRemoveAsync(dockerCliPath, containerName);
            TryDelete(jobRoot);
        }
    }

    private static async Task TryDockerKillAsync(string dockerCliPath, string containerName)
    {
        await RunDockerCommandBestEffortAsync(dockerCliPath, new[] { "kill", containerName });
    }

    private static async Task TryDockerRemoveAsync(string dockerCliPath, string containerName)
    {
        await RunDockerCommandBestEffortAsync(dockerCliPath, new[] { "rm", "-f", containerName });
    }

    private static async Task RunDockerCommandBestEffortAsync(string dockerCliPath, IEnumerable<string> arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = dockerCliPath;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            process.Start();
            await process.StandardOutput.ReadToEndAsync();
            await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
        }
        catch
        {
            // Best-effort cleanup/copy helper: failures here must never mask the
            // actual compile result, and are covered by TryDelete(jobRoot) + the
            // container's own lifecycle if `docker rm` itself fails.
        }
    }

    private async Task<List<CompileSimulationError>> ValidateRequestAsync(
        CompileSimulationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<CompileSimulationError>();

        if (!SupportedBoards.ContainsKey(request.Board))
        {
            errors.Add(new CompileSimulationError
            {
                Message = $"Unsupported board '{request.Board}'. Supported boards: {string.Join(", ", SupportedBoards.Keys)}."
            });
        }

        var sourceCode = GetSourceCode(request);
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            errors.Add(new CompileSimulationError { Message = "Source code is required." });
        }
        else if (sourceCode.Length > MaxCodeLength)
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

    private static string GetSourceCode(CompileSimulationRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.SourceCode)
            ? request.SourceCode
            : request.Code;
    }

    private static string NormalizeBoard(string board)
    {
        return SupportedBoards.TryGetValue(board, out var normalized)
            ? normalized
            : board;
    }

    private static int FirmwareSortOrder(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".bin" => 0,
            ".hex" => 1,
            ".elf" => 2,
            _ => 3
        };
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

    private static CompileSimulationResponse BuildServiceUnavailable(string jobId, string dockerCliPath)
    {
        return new CompileSimulationResponse
        {
            Success = false,
            JobId = jobId,
            Errors = new[]
            {
                new CompileSimulationError
                {
                    Message = $"Docker is not configured or not found at '{dockerCliPath}'."
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
