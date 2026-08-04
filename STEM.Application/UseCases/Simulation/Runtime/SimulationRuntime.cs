namespace STEM.Application.UseCases.Simulation.Runtime;

public class SimulationRuntime
{
    private readonly Dictionary<string, object> _state = new();

    public void SetVariable(string name, object value) => _state[name] = value;
    public T? GetVariable<T>(string name) => _state.TryGetValue(name, out var v) ? (T?)v : default;
    public void Clear() => _state.Clear();
    public IReadOnlyDictionary<string, object> GetState() => _state;
}
