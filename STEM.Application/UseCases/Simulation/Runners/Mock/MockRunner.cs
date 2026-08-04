namespace STEM.Application.UseCases.Simulation.Runners.Mock;

using STEM.Application.UseCases.Simulation.Abstractions;

public class MockRunner : ISimulationRunner
{
    public Task<RunResult> RunAsync(string code, CancellationToken ct = default)
    {
        return Task.FromResult(new RunResult { Success = true, Output = "[Mock] OK", ExitCode = 0, Duration = TimeSpan.Zero });
    }

    public bool ValidateCode(string code, out string? error) { error = null; return true; }
}
