namespace STEM.Application.UseCases.Simulation.Abstractions;

// Theo dõi CancellationTokenSource của mỗi lần chạy simulation nền đang diễn
// ra, keyed theo projectId — cho phép POST /stop hủy thật một lần chạy cụ
// thể (thay vì chỉ đổi Status trong DB). Singleton: phải sống xuyên suốt
// vòng đời app, không theo từng request/scope.
public interface IRunningSimulationRegistry
{
    void Register(string projectId, CancellationTokenSource cts);

    // CLOSE REMAINING QEMU RUNTIME STABILITY GAPS (BUG B — zombie container
    // trên rapid Run→Stop→Run): Register() (trên) chỉ Cancel() CTS cũ rồi
    // Dispose() ngay — KHÔNG chờ background task cũ (ExecuteInBackgroundAsync)
    // thực sự quan sát cancellation, kill container, và chạy xong finally
    // (TryDockerRemoveAsync/_registry.Remove) — xác nhận thật bằng
    // `docker ps` sau bài test rapid Run→Stop x5: 1 container "Up 15+ phút"
    // dù registry đã "Register" đè lên từ lâu. RegisterAsync là điểm ownership
    // đúng để đóng race này: nhận cả "startRun" (factory tạo Task chạy nền)
    // để CANCEL + AWAIT CLEANUP CỦA SESSION CŨ và START SESSION MỚI xảy ra
    // NGUYÊN TỬ dưới 1 SemaphoreSlim theo từng projectId (không khoá chéo
    // giữa các project khác nhau) — đây là "completion primitive thật" theo
    // yêu cầu, không phải Task.Delay đoán mò. Đồng thời đóng luôn race
    // concurrent-start (2 lần Start gần như đồng thời cho CÙNG 1 project):
    // lệnh Start thứ 2 phải đợi tới khi lệnh thứ 1 đã thật sự start xong (Task
    // đã được lưu vào registry) mới được xử lý, nên không bao giờ có 2 session
    // cùng "active" một lúc cho 1 project.
    Task RegisterAsync(string projectId, CancellationTokenSource cts, Func<Task> startRun);

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
