using System.Collections.Concurrent;
using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;

namespace STEM.Application.UseCases.Simulation.Runtime;

// Single-flight: nếu 2 caller (vd background precompile lúc học sinh gõ code,
// và QemuEsp32Runner cache-miss lúc bấm Run) cùng yêu cầu compile CÙNG 1 nội
// dung (cùng cacheKey — sourceCode+board+framework+fqbn+instrumentationVersion,
// xem FirmwareCacheService) gần như đồng thời, chỉ 1 lần compile thật chạy —
// caller đến sau chỉ AWAIT lại đúng Task đó, không khởi 2 compile song song
// (tránh đúng bug lẫn dữ liệu đã phát hiện khi 2 compile ghi chung 1
// build-path — never xảy ra nữa vì giờ chỉ có 1 compile thật cho 1 cacheKey
// tại 1 thời điểm).
//
// Task nền LUÔN chạy tới khi xong (CancellationToken.None) dù caller nào đó
// bỏ cuộc giữa chừng — compile ~40-90s không nên bị huỷ nửa chừng chỉ vì 1
// trong nhiều caller mất hứng (Stop/đóng tab), vì kết quả vẫn có giá trị cho
// firmware cache dùng về sau. `Task.WaitAsync(cancellationToken)` cho phép
// TỪNG caller dừng CHỜ độc lập mà không ảnh hưởng caller khác/compile thật.
public interface ICompileCoordinator
{
    Task<CompileSimulationResponse> GetOrCompileAsync(
        string cacheKey,
        Func<Task<CompileSimulationResponse>> compileFunc,
        CancellationToken cancellationToken);
}

public sealed class CompileCoordinator : ICompileCoordinator
{
    private readonly ConcurrentDictionary<string, Lazy<Task<CompileSimulationResponse>>> _inFlight = new();

    public Task<CompileSimulationResponse> GetOrCompileAsync(
        string cacheKey,
        Func<Task<CompileSimulationResponse>> compileFunc,
        CancellationToken cancellationToken)
    {
        // Lazy<Task<T>> (không phải trực tiếp GetOrAdd(key, _ => compileFunc()))
        // là bắt buộc — ConcurrentDictionary.GetOrAdd có thể gọi factory nhiều
        // lần khi có race, chỉ giữ lại 1 kết quả; nếu factory là chính compile
        // thật thì race đó = 2 compile thật cùng chạy (đúng bug muốn né). Lazy
        // với chế độ mặc định (ExecutionAndPublication) đảm bảo factory chỉ
        // thực thi ĐÚNG 1 LẦN dù nhiều thread cùng gọi .Value.
        var lazy = _inFlight.GetOrAdd(
            cacheKey,
            _ => new Lazy<Task<CompileSimulationResponse>>(() => RunAndUntrackAsync(cacheKey, compileFunc)));

        return lazy.Value.WaitAsync(cancellationToken);
    }

    private async Task<CompileSimulationResponse> RunAndUntrackAsync(
        string cacheKey,
        Func<Task<CompileSimulationResponse>> compileFunc)
    {
        try
        {
            return await compileFunc();
        }
        finally
        {
            _inFlight.TryRemove(cacheKey, out _);
        }
    }
}
