namespace STEM.Application.UseCases.Simulation.Runners.Educational;

using STEM.Application.UseCases.Simulation.Abstractions;

public class EducationalRunner : ISimulationRunner
{
    public async Task<RunResult> RunAsync(string code, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return new RunResult { Success = true, Output = "Simulated", ExitCode = 0, Duration = TimeSpan.FromMilliseconds(100) };
    }

    public bool ValidateCode(string code, out string? error) { error = null; return true; }
}
