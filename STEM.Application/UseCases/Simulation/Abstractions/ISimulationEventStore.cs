using STEM.Application.Dtos.Simulation;

namespace STEM.Application.UseCases.Simulation.Abstractions;

// Ghi SimulationEvent xuống VirtualLabProject.SimulationEventsJson ngay khi
// tính ra (atomic JSONB append, tái dùng đúng pattern đã có từ Giai đoạn 4 —
// xem VirtualLabRuntimeService.AppendSimulationEventAsync). Định nghĩa ở
// STEM.Application để EducationalSimulationRunner không phải phụ thuộc
// ngược vào StemDbContext (STEM.Infrastructure) — implementation thật nằm ở
// Infrastructure, resolve qua scope riêng cho mỗi lần chạy nền (Scoped,
// không thể inject thẳng vào EducationalSimulationRunner vốn là Singleton).
public interface ISimulationEventStore
{
    Task AppendEventAsync(string projectId, SimulationEventResponse evt, CancellationToken cancellationToken);

    Task MarkRunFinishedAsync(string projectId, string status, CancellationToken cancellationToken);
}
