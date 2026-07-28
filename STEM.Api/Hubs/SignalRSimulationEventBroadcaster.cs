using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Infrastructure.Data;

namespace STEM.Api.Hubs;

// Implementation thật của ISimulationEventBroadcaster (định nghĩa ở
// STEM.Application) — chỉ project này biết VirtualLabHub, nên adapter sống
// ở đây. IHubContext<VirtualLabHub> là Singleton-safe, an toàn để
// EducationalSimulationRunner/QemuEsp32Runner (Singleton) giữ tham chiếu
// trực tiếp, không cần resolve qua scope như ISimulationEventStore
// (StemDbContext). Các method Compile* MỚI cần tra classId (StemDbContext,
// Scoped) nên resolve qua IServiceScopeFactory riêng cho lần gọi đó — không
// đụng tới constructor Singleton hiện có.
public sealed class SignalRSimulationEventBroadcaster : ISimulationEventBroadcaster
{
    private readonly IHubContext<VirtualLabHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public SignalRSimulationEventBroadcaster(IHubContext<VirtualLabHub> hubContext, IServiceScopeFactory scopeFactory)
    {
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public Task BroadcastEventAsync(string projectId, SimulationEventResponse evt, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.Group(ProjectGroup(projectId)).SendAsync(
            "StudentSimulationEvent",
            NormalizeProjectId(projectId),
            evt,
            cancellationToken);
    }

    public async Task BroadcastRunCompletedAsync(string projectId, string status, string? reason, CancellationToken cancellationToken)
    {
        var normalizedProjectId = NormalizeProjectId(projectId);
        await SendToProjectAndClassAsync(
            normalizedProjectId,
            "StudentRunCompleted",
            [normalizedProjectId, status, reason],
            cancellationToken);
    }

    public async Task BroadcastCompileStartedAsync(string projectId, CancellationToken cancellationToken)
    {
        var normalizedProjectId = NormalizeProjectId(projectId);
        await SendToProjectAndClassAsync(
            normalizedProjectId,
            "StudentCompileStarted",
            [normalizedProjectId],
            cancellationToken);
    }

    public async Task BroadcastCompileFinishedAsync(string projectId, bool success, string? errorSummary, CancellationToken cancellationToken)
    {
        var normalizedProjectId = NormalizeProjectId(projectId);
        await SendToProjectAndClassAsync(
            normalizedProjectId,
            "StudentCompileFinished",
            [normalizedProjectId, success, errorSummary],
            cancellationToken);
    }

    public async Task BroadcastRunBootingAsync(string projectId, CancellationToken cancellationToken)
    {
        var normalizedProjectId = NormalizeProjectId(projectId);
        await SendToProjectAndClassAsync(
            normalizedProjectId,
            "StudentRunBooting",
            [normalizedProjectId],
            cancellationToken);
    }

    // Compile-started/finished/RunBooting/RunCompleted phải tới CẢ project
    // group lẫn class group — Teacher ClassMonitorPage.tsx theo dõi cả lớp
    // qua WatchClass (chỉ join "class-{id}"), KHÔNG join riêng từng
    // "project-{id}" trừ khi bấm "Xem live" (WatchStudent). Thiếu class
    // group ở đây thì dashboard lớp học không bao giờ thấy trạng thái
    // "đang chạy"/"đã dừng" của học sinh — chỉ thấy compiling (bug thật,
    // phát hiện lúc làm 4.5, xem VIRTUAL_LAB_PLAN.md). BroadcastEventAsync
    // (StudentSimulationEvent, từng tick GPIO) CỐ Ý vẫn chỉ project group —
    // tần suất quá cao để phát cho cả lớp, chỉ ai đang "Xem live" 1 học sinh
    // cụ thể mới cần chi tiết đó.
    private async Task SendToProjectAndClassAsync(
        string normalizedProjectId,
        string method,
        object?[] args,
        CancellationToken cancellationToken)
    {
        var classId = await ResolveClassIdAsync(normalizedProjectId, cancellationToken);
        var groups = classId.HasValue
            ? new[] { ClassGroup(classId.Value), $"project-{normalizedProjectId}" }
            : new[] { $"project-{normalizedProjectId}" };

        await _hubContext.Clients.Groups(groups).SendCoreAsync(method, args, cancellationToken);
    }

    // Lặp lại đúng logic ResolveStudentClassIdAsync của VirtualLabHub (không
    // tái dùng trực tiếp được — khác project/layer, Hub method còn cần
    // Context.ConnectionId để join group mà broadcaster này không có). Chỉ
    // lấy classId đầu tiên khớp (cùng quy ước "nhiều lớp khớp → chọn nhỏ
    // nhất" như Hub), không log warning trùng lặp ở đây vì Hub đã log rồi
    // lúc JoinSession.
    private async Task<int?> ResolveClassIdAsync(string normalizedProjectId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(normalizedProjectId, out var id))
        {
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StemDbContext>();

        var project = await context.VirtualLabProjects
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.LabId, item.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (project?.LabId == null || project.UserId == null)
        {
            return null;
        }

        return await (
            from assignment in context.LabClassAssignments.AsNoTracking()
            join enrollment in context.Enrollments.AsNoTracking()
                on assignment.ClassId equals enrollment.ClassId
            where assignment.LabId == project.LabId.Value &&
                  enrollment.StudentId == project.UserId.Value
            orderby assignment.ClassId
            select (int?)assignment.ClassId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string ClassGroup(int classId) => $"class-{classId}";

    // Phải khớp CHÍNH XÁC quy ước group name của VirtualLabHub.ProjectGroup —
    // 2 nơi định nghĩa độc lập (không share code được vì khác project/layer),
    // sai lệch ở đây sẽ khiến broadcast "thành công" nhưng không ai nhận
    // được (gửi nhầm group), lỗi âm thầm rất khó phát hiện qua log.
    private static string ProjectGroup(string projectId) => $"project-{NormalizeProjectId(projectId)}";

    private static string NormalizeProjectId(string projectId)
    {
        return Guid.TryParse(projectId, out var id) ? id.ToString("N") : projectId;
    }
}
