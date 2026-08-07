using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Runtime;

public sealed class SimulationRunResult
{
    public bool Success { get; init; }
    public IReadOnlyCollection<SimulationEventResponse> Events { get; init; } = Array.Empty<SimulationEventResponse>();
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();

    public static SimulationRunResult Failed(params string[] errors)
    {
        return new SimulationRunResult
        {
            Success = false,
            Events = errors.Select(error => new SimulationEventResponse
            {
                Type = "error",
                Time = 0,
                Payload = new Dictionary<string, object?>
                {
                    ["message"] = error
                }
            }).ToList(),
            Errors = errors
        };
    }
}
