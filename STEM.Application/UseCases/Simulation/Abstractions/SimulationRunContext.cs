using System.Collections.Concurrent;

namespace STEM.Application.UseCases.Simulation.Abstractions;

public sealed class SimulationRunContext
{
    public required string ProjectId { get; init; }
    public required string SourceCode { get; init; }
    public required string DiagramJson { get; init; }
    public required string Mode { get; init; }
    public int MaxDurationMs { get; init; } = 5000;
    public int MaxInstructionCount { get; init; } = 10000;

    // ConcurrentDictionary (not Dictionary) on purpose — a runner reading this
    // on its own background thread (e.g. EducationalEventGenerator's
    // digitalRead handling) can race with ISimulationInputChannel writing a
    // live input value from a SignalR Hub call on a different thread.
    // ConcurrentDictionary still satisfies IReadOnlyDictionary<string, object>
    // (ButtonModel.Read's parameter type), so no reader-side changes needed.
    public ConcurrentDictionary<string, object> ComponentInputs { get; init; } = new();
}
