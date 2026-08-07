namespace STEM.Application.UseCases.Simulation.Abstractions;

// Theo dõi CancellationTokenSource của mỗi lần chạy simulation nền đang diễn
// ra, keyed theo projectId — cho phép POST /stop hủy thật một lần chạy cụ
// thể (thay vì chỉ đổi Status trong DB). Singleton: phải sống xuyên suốt
// vòng đời app, không theo từng request/scope.
public interface IRunningSimulationRegistry
{
    void Register(string projectId, CancellationTokenSource cts);

    bool TryCancel(string projectId);

    void Remove(string projectId);

    // Kiểm tra không phá hủy trạng thái — dùng để CHẶN 1 hành động khác (vd
    // Submit) khi đang có lần chạy nền thật sự diễn ra, KHÔNG dùng
    // VirtualLabProject.Status cho việc này: "Running" trong DB vừa có
    // nghĩa "đang chạy nền" vừa có nghĩa "lần chạy trước hoàn tất không
    // lỗi" (không có trạng thái "completed" riêng) — registry mới là nguồn
    // sự thật chính xác cho "có đang thực sự chạy hay không".
    bool IsRunning(string projectId);
}
