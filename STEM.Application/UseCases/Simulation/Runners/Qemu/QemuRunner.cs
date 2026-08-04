namespace STEM.Application.UseCases.Simulation.Runners.Qemu;

using STEM.Application.UseCases.Simulation.Abstractions;

public class QemuRunner : ISimulationRunner
{
    public async Task<RunResult> RunAsync(string code, CancellationToken ct = default)
    {
        await Task.Delay(500, ct);
        return new RunResult { Success = true, Output = "[QEMU] OK", ExitCode = 0, Duration = TimeSpan.FromMilliseconds(500) };
    }

    public bool ValidateCode(string code, out string? error) { error = null; return true; }
}
