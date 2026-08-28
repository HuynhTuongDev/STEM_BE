using System.Collections.Concurrent;

namespace STEM.Application.UseCases.Simulation.Abstractions;

public enum SimulationInputType
{
    Digital,
    Analog,
    Sensor
}

// Conceptual shape (STEP 2): { projectId, componentId, inputType, pin, value }.
// Provider-agnostic on purpose — never bound to Fritzing/KiCad or any external
// component id, only to the diagram part id (componentId) already used inside
// a running simulation (matches ButtonModel.PartId).
// SensorKind is Sensor-only metadata (e.g. "light", "distance") — future
// sensor types add a new SensorKind value, not a new event shape or a new
// generic "extra data" bag. Digital/Analog leave it null; nothing about how
// TrySetInput stores or ButtonModel/PotentiometerModel/LightSensorModel read
// a value depends on it today.
public sealed record SimulationInputEvent(
    string ProjectId,
    string ComponentId,
    string? Pin,
    SimulationInputType InputType,
    object Value,
    string? SensorKind = null);

// Runner-independent bridge between "a session is currently running" and
// "someone wants to push a live input value into it". A runner that supports
// dynamic input registers its own mutable per-session input store here right
// before starting execution (RegisterSession) and unregisters it when the run
// ends (UnregisterSession) — same lifecycle shape as IRunningSimulationRegistry.
// A runner that CANNOT support this (QEMU, today) simply never registers, so
// TrySetInput correctly reports "no running session" for it without any
// QEMU-specific code here.
public interface ISimulationInputChannel
{
    void RegisterSession(string projectId, ConcurrentDictionary<string, object> inputs);

    void UnregisterSession(string projectId);

    // False when no session is currently registered for this projectId — either
    // it was never a dynamic-input-capable runner, it already stopped, or the
    // projectId is simply wrong. Never throws for "not found"; the Hub decides
    // how to surface that.
    bool TrySetInput(SimulationInputEvent inputEvent);
}
