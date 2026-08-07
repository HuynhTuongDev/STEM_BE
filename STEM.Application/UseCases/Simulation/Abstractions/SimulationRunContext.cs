namespace STEM.Application.UseCases.Simulation.Abstractions;

public sealed class SimulationRunContext
{
    public required string ProjectId { get; init; }
    public required string SourceCode { get; init; }
    public required string DiagramJson { get; init; }
    public required string Mode { get; init; }
    public int MaxDurationMs { get; init; } = 5000;
    public int MaxInstructionCount { get; init; } = 10000;
    public Dictionary<string, object> ComponentInputs { get; init; } = new();
}
