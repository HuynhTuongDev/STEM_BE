using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Abstractions;

// Đẩy SimulationEvent qua kênh realtime (SignalR) ngay khi nó được tính ra
// ở tầng nền — thay cho việc FE tự relay event mà chính nó vừa nhận qua
// response HTTP đồng bộ (không còn tồn tại ở model streaming). Định nghĩa ở
// STEM.Application để EducationalSimulationRunner (Application) không phải
// biết tới VirtualLabHub (STEM.Api) — implementation thật nằm ở STEM.Api,
// nơi VirtualLabHub được định nghĩa.
public interface ISimulationEventBroadcaster
{
    Task BroadcastEventAsync(string projectId, SimulationEventResponse evt, CancellationToken cancellationToken);

    Task BroadcastRunCompletedAsync(string projectId, string status, string? reason, CancellationToken cancellationToken);

    // 3 tín hiệu giai đoạn cho UX loading rõ ràng khi mode="qemu" (compile thật
    // ~40-90s + boot QEMU ~4s trước khi có event mô phỏng đầu tiên). Compile-
    // started/finished TÁI DÙNG đúng tên broadcast "StudentCompileStarted"/
    // "StudentCompileFinished" mà VirtualLabHub.CompileStarted/CompileFinished
    // (hub method cũ, học sinh tự gọi từ FE trước khi có lời gọi compile() ở
    // FE) đã phát ra từ trước — ClassMonitorPage.tsx (Dashboard giáo viên) đã
    // lắng nghe sẵn 2 event này, không cần đổi gì bên đó. Nay compile chỉ còn
    // xảy ra trong QemuEsp32Runner (BE), nên chính nó phải tự phát trực tiếp
    // qua đây thay vì chờ FE gọi hộ.
    Task BroadcastCompileStartedAsync(string projectId, CancellationToken cancellationToken);

    Task BroadcastCompileFinishedAsync(string projectId, bool success, string? errorSummary, CancellationToken cancellationToken);

    // Giai đoạn QEMU boot (~4s, sau compile xong, trước sự kiện mô phỏng đầu
    // tiên) — không có khái niệm tương đương trước đây (EducationalSimulationRunner
    // không có bước boot), nên đặt tên mới, khớp quy ước StudentRunStarted/
    // StudentRunCompleted đã có.
    Task BroadcastRunBootingAsync(string projectId, CancellationToken cancellationToken);
}
