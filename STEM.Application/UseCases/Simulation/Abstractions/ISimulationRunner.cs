namespace STEM.Application.UseCases.Simulation.Abstractions;

public interface ISimulationRunner
{
    Task<RunResult> RunAsync(string code, CancellationToken ct = default);
    bool ValidateCode(string code, out string? error);
}

public class RunResult
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public int ExitCode { get; set; }
    public TimeSpan Duration { get; set; }
}
