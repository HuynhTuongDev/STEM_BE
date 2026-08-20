using System.Collections.Concurrent;
using STEM.Application.UseCases.Simulation.Abstractions;

namespace STEM.Application.UseCases.Simulation.Runtime;

public sealed class SimulationInputChannel : ISimulationInputChannel
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _sessions = new();

    public void RegisterSession(string projectId, ConcurrentDictionary<string, object> inputs)
    {
        _sessions[projectId] = inputs;
    }

    public void UnregisterSession(string projectId)
    {
        _sessions.TryRemove(projectId, out _);
    }

    public bool TrySetInput(SimulationInputEvent inputEvent)
    {
        if (!_sessions.TryGetValue(inputEvent.ProjectId, out var inputs))
        {
            return false;
        }

        // Keyed by componentId (diagram part id) — matches ButtonModel.Read's
        // first lookup (PartId), which is checked before GpioPin/"pin:{Gpio}".
        // Digital values are stored as bool so ButtonModel.NormalizeInput's
        // `bool pressed` branch matches directly, no string parsing needed on
        // the read side.
        inputs[inputEvent.ComponentId] = inputEvent.Value;
        return true;
    }
}
