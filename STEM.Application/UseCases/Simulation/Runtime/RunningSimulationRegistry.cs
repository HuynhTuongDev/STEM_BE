using System.Collections.Concurrent;
using STEM.Application.UseCases.Simulation.Abstractions;

namespace STEM.Application.UseCases.Simulation.Runtime;

public sealed class RunningSimulationRegistry : IRunningSimulationRegistry
{
    // Entry gộp CTS + Task nền (nếu đã start xong) — RunTask null nghĩa là
    // slot vừa được RegisterAsync chiếm nhưng startRun() chưa kịp trả về Task
    // (không thể xảy ra với implementation dưới đây vì startRun() chạy VÀ
    // gán RunTask trong CÙNG 1 khối critical section, nhưng vẫn khai báo
    // nullable để Register() (sync, không có Task nền) dùng chung type).
    private sealed class Entry
    {
        public Entry(CancellationTokenSource cts, Task? runTask = null)
        {
            Cts = cts;
            RunTask = runTask;
        }

        public CancellationTokenSource Cts { get; }
        public Task? RunTask { get; }
    }

    private readonly ConcurrentDictionary<string, Entry> _running = new();
    // 1 SemaphoreSlim riêng cho MỖI projectId — nghiêm ngặt hoá đúng phạm vi
    // "project đang được Start/thay thế", KHÔNG khoá chéo giữa các project
    // khác nhau (2 học sinh Start lab khác nhau cùng lúc không bị chặn nhau).
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public void Register(string projectId, CancellationTokenSource cts)
    {
        // Nếu project này đã có 1 lần chạy đăng ký từ trước (vd Run lần 2 khi
        // lần 1 chưa kịp Remove) mà cứ ghi đè thẳng, CTS cũ sẽ bị "mồ côi" —
        // không ai giữ tham chiếu để hủy nó nữa, background task cũ chạy mãi
        // không dừng được qua Stop. Hủy + dispose CTS cũ trước khi thay thế.
        // Giữ lại nguyên trạng (sync, không chờ cleanup) — dùng bởi
        // EducationalSimulationRunner, nơi "session cũ" không giữ resource hệ
        // điều hành (không Docker container) nên rủi ro zombie không áp dụng;
        // RunningSimulationRegistryTests.cs cũng pin đúng hành vi sync này.
        if (_running.TryGetValue(projectId, out var previous))
        {
            SafeCancel(previous.Cts);
            previous.Cts.Dispose();
        }

        _running[projectId] = new Entry(cts);
    }

    public async Task RegisterAsync(string projectId, CancellationTokenSource cts, Func<Task> startRun)
    {
        var gate = _locks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            // Cancel + CHỜ THẬT session cũ (nếu có) dọn xong (kill QEMU
            // process, docker rm -f container, chạy hết finally của
            // ExecuteInBackgroundAsync) TRƯỚC khi session mới được coi là
            // "active" — đúng invariant "mỗi project tối đa 1 active
            // runner/container tại 1 thời điểm" (PHASE H). Toàn bộ khối này
            // nằm dưới `gate` nên 2 lệnh Start gần như đồng thời cho CÙNG
            // project bị serialize hoàn toàn — lệnh thứ 2 luôn thấy Task của
            // lệnh thứ 1 đã được gán (start+gán Task cũng nằm trong critical
            // section này) và đợi nó dọn xong trước khi tự start.
            if (_running.TryGetValue(projectId, out var previous))
            {
                SafeCancel(previous.Cts);
                if (previous.RunTask != null)
                {
                    try
                    {
                        await previous.RunTask;
                    }
                    catch
                    {
                        // ExecuteInBackgroundAsync của cả 2 runner đã tự bắt
                        // và phân loại MỌI exception của chính nó
                        // (catch(OperationCanceledException)/catch(Exception)
                        // riêng, ghi finalStatus rồi return bình thường) — Task
                        // này chỉ throw nếu có lỗi bất thường ở tầng ngoài. Ở
                        // đây ta chỉ cần biết nó ĐÃ XONG (để đảm bảo container
                        // cũ đã terminated trước khi session mới bắt đầu),
                        // không cần xử lý lỗi của nó lần nữa — không còn ai
                        // để throw tới (không phải request HTTP gốc).
                    }
                }

                previous.Cts.Dispose();
            }

            var runTask = startRun();
            _running[projectId] = new Entry(cts, runTask);
        }
        finally
        {
            gate.Release();
        }
    }

    public bool TryCancel(string projectId)
    {
        if (_running.TryGetValue(projectId, out var entry))
        {
            SafeCancel(entry.Cts);
            return true;
        }

        return false;
    }

    public void Remove(string projectId)
    {
        if (_running.TryRemove(projectId, out var entry))
        {
            entry.Cts.Dispose();
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
