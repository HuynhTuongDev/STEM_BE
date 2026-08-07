using System.Collections.Concurrent;
using STEM.Application.UseCases.Simulation.Abstractions;

namespace STEM.Application.UseCases.Simulation.Runtime;

public sealed class RunningSimulationRegistry : IRunningSimulationRegistry
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _running = new();

    public void Register(string projectId, CancellationTokenSource cts)
    {
        // Nếu project này đã có 1 lần chạy đăng ký từ trước (vd Run lần 2 khi
        // lần 1 chưa kịp Remove) mà cứ ghi đè thẳng, CTS cũ sẽ bị "mồ côi" —
        // không ai giữ tham chiếu để hủy nó nữa, background task cũ chạy mãi
        // không dừng được qua Stop. Hủy + dispose CTS cũ trước khi thay thế.
        if (_running.TryGetValue(projectId, out var previous))
        {
            SafeCancel(previous);
            previous.Dispose();
        }

        _running[projectId] = cts;
    }

    public bool TryCancel(string projectId)
    {
        if (_running.TryGetValue(projectId, out var cts))
        {
            SafeCancel(cts);
            return true;
        }

        return false;
    }

    public void Remove(string projectId)
    {
        if (_running.TryRemove(projectId, out var cts))
        {
            cts.Dispose();
        }
    }

    public bool IsRunning(string projectId) => _running.ContainsKey(projectId);

    private static void SafeCancel(CancellationTokenSource cts)
    {
        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Đã bị dispose bởi Remove() ở nhánh khác gần như đồng thời — coi
            // như đã hủy xong, không phải lỗi.
        }
    }
}
