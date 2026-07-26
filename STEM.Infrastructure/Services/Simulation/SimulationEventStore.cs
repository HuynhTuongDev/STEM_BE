using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Core.Entities.Simulations;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services.Simulation;

// Scoped: mỗi lần chạy nền (EducationalSimulationRunner.ExecuteInBackgroundAsync)
// tự tạo 1 IServiceScope riêng để resolve instance này — StemDbContext không
// sống sót qua nhiều lần dùng chồng chéo giữa các lần chạy khác nhau.
public sealed class SimulationEventStore : ISimulationEventStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly StemDbContext _context;

    public SimulationEventStore(StemDbContext context)
    {
        _context = context;
    }

    public async Task AppendEventAsync(string projectId, SimulationEventResponse evt, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new ArgumentException("projectId must be a GUID virtual-lab project id.", nameof(projectId));
        }

        var eventBatchJson = JsonSerializer.Serialize(new[] { evt }, JsonOptions);

        // Cùng pattern atomic JSONB append đã dùng ở
        // VirtualLabRuntimeService.AppendSimulationEventAsync (Giai đoạn 4) —
        // không đọc-sửa-ghi trong C#, không race giữa nhiều event bắn gần
        // như đồng thời.
        await _context.Database.ExecuteSqlRawAsync(
            """
            UPDATE "VirtualLabProjects"
            SET
                "SimulationEventsJson" = "SimulationEventsJson" || @eventBatch,
                "UpdatedAt" = @updatedAt
            WHERE "Id" = @projectId
            """,
            [
                new NpgsqlParameter("eventBatch", NpgsqlDbType.Jsonb) { Value = eventBatchJson },
                new NpgsqlParameter("updatedAt", NpgsqlDbType.TimestampTz) { Value = DateTime.UtcNow },
                new NpgsqlParameter("projectId", NpgsqlDbType.Uuid) { Value = id }
            ],
            cancellationToken);
        // Không throw khi affected=0 (project bị xoá giữa lúc đang chạy nền,
        // vd trường hợp hiếm) — best-effort, lỗi ở đây không có request HTTP
        // nào để trả về nữa, chỉ nên bỏ qua và để lần AppendEvent kế tiếp
        // (hoặc MarkRunFinishedAsync) tự nhiên cũng no-op tương tự.
    }

    public async Task MarkRunFinishedAsync(string projectId, string status, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new ArgumentException("projectId must be a GUID virtual-lab project id.", nameof(projectId));
        }

        var normalizedStatus = VirtualLabProjectStatuses.All.Contains(status)
            ? status
            : VirtualLabProjectStatuses.Error;

        await _context.Database.ExecuteSqlRawAsync(
            """
            UPDATE "VirtualLabProjects"
            SET
                "Status" = @status,
                "UpdatedAt" = @updatedAt
            WHERE "Id" = @projectId
            """,
            [
                new NpgsqlParameter("status", NpgsqlDbType.Varchar) { Value = normalizedStatus },
                new NpgsqlParameter("updatedAt", NpgsqlDbType.TimestampTz) { Value = DateTime.UtcNow },
                new NpgsqlParameter("projectId", NpgsqlDbType.Uuid) { Value = id }
            ],
            cancellationToken);
    }
}
