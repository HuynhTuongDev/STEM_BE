using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using STEM.Application.Interfaces;
using STEM.Infrastructure.Data;

namespace STEM.Api.Hubs;

[Authorize]
public class VirtualLabHub : Hub
{
    private static readonly ConcurrentDictionary<string, SessionContext> SessionsByConnection = new();
    private const string ProjectMissingLabMessage = "Project chưa liên kết với bài lab, không xác định được quyền xem.";

    private readonly IVirtualLabRuntimeService _runtimeService;
    private readonly StemDbContext _context;
    private readonly ILogger<VirtualLabHub> _logger;

    public VirtualLabHub(
        IVirtualLabRuntimeService runtimeService,
        StemDbContext context,
        ILogger<VirtualLabHub> logger)
    {
        _runtimeService = runtimeService;
        _context = context;
        _logger = logger;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (SessionsByConnection.TryRemove(Context.ConnectionId, out var session))
        {
            if (session.ClassId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, ClassGroup(session.ClassId.Value));
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(session.ProjectId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSession(string projectId)
    {
        await JoinStudentSessionAsync(projectId, notifyClassWatchers: true);
    }

    public async Task DiagramUpdated(string projectId, string diagramJson)
    {
        _ = await GetOrJoinSessionAsync(projectId);
        await Clients.Group(ProjectGroup(projectId)).SendAsync(
            "StudentDiagramUpdated",
            projectId,
            diagramJson,
            Context.ConnectionAborted);
    }

    public async Task CodeUpdated(string projectId, string sourceCode)
    {
        _ = await GetOrJoinSessionAsync(projectId);
        await Clients.Group(ProjectGroup(projectId)).SendAsync(
            "StudentCodeUpdated",
            projectId,
            sourceCode,
            Context.ConnectionAborted);
    }

    public async Task CompileStarted(string projectId)
    {
        var session = await GetOrJoinSessionAsync(projectId);
        await Clients.Groups(SessionGroups(session)).SendAsync(
            "StudentCompileStarted",
            projectId,
            Context.ConnectionAborted);
    }

    public async Task CompileFinished(string projectId, bool success, string? errorSummary)
    {
        var session = await GetOrJoinSessionAsync(projectId);
        await Clients.Groups(SessionGroups(session)).SendAsync(
            "StudentCompileFinished",
            projectId,
            success,
            errorSummary,
            Context.ConnectionAborted);
    }

    public async Task RunStarted(string projectId)
    {
        _ = await GetOrJoinSessionAsync(projectId);
        await _runtimeService.MarkRunStartedAsync(projectId, GetCurrentUserId(), Context.ConnectionAborted);
        await Clients.Group(ProjectGroup(projectId)).SendAsync(
            "StudentRunStarted",
            projectId,
            Context.ConnectionAborted);
    }

    public async Task SimulationEvent(string projectId, object eventPayload)
    {
        _ = await GetOrJoinSessionAsync(projectId);
        await _runtimeService.AppendSimulationEventAsync(projectId, eventPayload, GetCurrentUserId(), Context.ConnectionAborted);
        await Clients.Group(ProjectGroup(projectId)).SendAsync(
            "StudentSimulationEvent",
            projectId,
            eventPayload,
            Context.ConnectionAborted);
    }

    public async Task Stopped(string projectId)
    {
        var session = await GetOrJoinSessionAsync(projectId);
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new HubException("projectId must be a GUID virtual-lab project id.");
        }

        var stopped = await _runtimeService.StopSimulationAsync(id, GetCurrentUserId(), Context.ConnectionAborted);
        if (!stopped)
        {
            throw new HubException("VirtualLabProject not found.");
        }

        await Clients.Groups(SessionGroups(session)).SendAsync(
            "StudentStopped",
            projectId,
            Context.ConnectionAborted);
    }

    public async Task Submitted(string projectId, int submissionId)
    {
        var session = await GetOrJoinSessionAsync(projectId);
        await Clients.Groups(SessionGroups(session)).SendAsync(
            "StudentSubmitted",
            projectId,
            submissionId,
            Context.ConnectionAborted);
    }

    public async Task WatchStudent(string projectId)
    {
        await EnsureTeacherCanWatchProjectAsync(projectId);
        await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectId));
    }

    public async Task UnwatchStudent(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(projectId));
    }

    public async Task WatchClass(int classId)
    {
        await EnsureTeacherCanWatchClassAsync(classId);
        await Groups.AddToGroupAsync(Context.ConnectionId, ClassGroup(classId));
    }

    public async Task UnwatchClass(int classId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ClassGroup(classId));
    }

    public async Task SendGuidance(string projectId, string message)
    {
        await EnsureTeacherCanWatchProjectAsync(projectId);
        await Clients.Group(ProjectGroup(projectId)).SendAsync(
            "ReceiveGuidance",
            message,
            GetCurrentUserName(),
            Context.ConnectionAborted);
    }

    private async Task<SessionContext> GetOrJoinSessionAsync(string projectId)
    {
        var normalizedProjectId = NormalizeProjectId(projectId);
        if (SessionsByConnection.TryGetValue(Context.ConnectionId, out var session) &&
            session.ProjectId.Equals(normalizedProjectId, StringComparison.OrdinalIgnoreCase))
        {
            return session;
        }

        _logger.LogInformation(
            "Auto-joining virtual lab session {ProjectId} for connection {ConnectionId} before processing a hub event.",
            normalizedProjectId,
            Context.ConnectionId);

        return await JoinStudentSessionAsync(projectId, notifyClassWatchers: false);
    }

    private async Task<SessionContext> JoinStudentSessionAsync(string projectId, bool notifyClassWatchers)
    {
        var normalizedProjectId = NormalizeProjectId(projectId);
        var studentId = GetCurrentUserId();
        var project = await _runtimeService.GetDiagramAsync(normalizedProjectId, studentId, Context.ConnectionAborted);
        if (project == null)
        {
            throw new HubException("VirtualLabProject not found.");
        }

        var classId = await ResolveStudentClassIdAsync(normalizedProjectId, studentId);

        if (SessionsByConnection.TryGetValue(Context.ConnectionId, out var previousSession))
        {
            if (previousSession.ClassId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, ClassGroup(previousSession.ClassId.Value));
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(previousSession.ProjectId));
        }

        var session = new SessionContext(normalizedProjectId, classId);
        SessionsByConnection[Context.ConnectionId] = session;

        await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(normalizedProjectId));

        if (classId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ClassGroup(classId.Value));

            if (notifyClassWatchers)
            {
                await Clients.OthersInGroup(ClassGroup(classId.Value)).SendAsync(
                    "StudentJoined",
                    projectId,
                    studentId,
                    GetCurrentUserName(),
                    Context.ConnectionAborted);
            }
        }

        return session;
    }

    private async Task<int?> ResolveStudentClassIdAsync(string projectId, int studentId)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new HubException("projectId must be a GUID virtual-lab project id.");
        }

        var project = await _context.VirtualLabProjects
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.LabId })
            .FirstOrDefaultAsync(Context.ConnectionAborted);

        if (project == null)
        {
            throw new HubException("VirtualLabProject not found.");
        }

        if (!project.LabId.HasValue)
        {
            _logger.LogWarning(
                "JoinSession could not resolve a class group because project {ProjectId} has no LabId. Student {StudentId} joined project group only.",
                projectId,
                studentId);
            return null;
        }

        var matchingClassIds = await (
            from assignment in _context.LabClassAssignments.AsNoTracking()
            join enrollment in _context.Enrollments.AsNoTracking()
                on assignment.ClassId equals enrollment.ClassId
            where assignment.LabId == project.LabId.Value &&
                  enrollment.StudentId == studentId
            orderby assignment.ClassId
            select assignment.ClassId)
            .Distinct()
            .ToListAsync(Context.ConnectionAborted);

        if (matchingClassIds.Count == 0)
        {
            _logger.LogWarning(
                "JoinSession could not resolve a class group for project {ProjectId}, lab {LabId}, student {StudentId}. Student joined project group only.",
                projectId,
                project.LabId.Value,
                studentId);
            return null;
        }

        var classId = matchingClassIds[0];
        if (matchingClassIds.Count > 1)
        {
            _logger.LogWarning(
                "JoinSession found multiple matching classes for project {ProjectId}, lab {LabId}, student {StudentId}. Using class {ClassId}. Matches: {MatchingClassIds}",
                projectId,
                project.LabId.Value,
                studentId,
                classId,
                string.Join(",", matchingClassIds));
        }

        return classId;
    }

    private async Task EnsureTeacherCanWatchProjectAsync(string projectId)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new HubException("projectId must be a GUID virtual-lab project id.");
        }

        var project = await _context.VirtualLabProjects
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.LabId })
            .FirstOrDefaultAsync(Context.ConnectionAborted);

        if (project == null)
        {
            throw new HubException("VirtualLabProject not found.");
        }

        if (!project.LabId.HasValue)
        {
            throw new HubException(ProjectMissingLabMessage);
        }

        var teacherId = GetCurrentUserId();
        var canWatch = await _context.LabClassAssignments
            .AsNoTracking()
            .AnyAsync(
                assignment =>
                    assignment.LabId == project.LabId.Value &&
                    assignment.Class!.TeacherId == teacherId,
                Context.ConnectionAborted);

        if (!canWatch)
        {
            throw new HubException("Bạn không có quyền xem project này.");
        }
    }

    private async Task EnsureTeacherCanWatchClassAsync(int classId)
    {
        var teacherId = GetCurrentUserId();
        var canWatch = await _context.Classes
            .AsNoTracking()
            .AnyAsync(item => item.Id == classId && item.TeacherId == teacherId, Context.ConnectionAborted);

        if (!canWatch)
        {
            throw new HubException("Bạn không có quyền xem lớp này.");
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new HubException("User is not authenticated.");
    }

    private string GetCurrentUserName()
    {
        return Context.User?.FindFirst(ClaimTypes.Name)?.Value
            ?? Context.User?.FindFirst("name")?.Value
            ?? $"user-{GetCurrentUserId()}";
    }

    private static string ClassGroup(int classId) => $"class-{classId}";

    private static string ProjectGroup(string projectId) => $"project-{NormalizeProjectId(projectId)}";

    private static string NormalizeProjectId(string projectId)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new HubException("projectId must be a GUID virtual-lab project id.");
        }

        return id.ToString("N");
    }

    private static string[] SessionGroups(SessionContext session)
    {
        return session.ClassId.HasValue
            ? [ClassGroup(session.ClassId.Value), ProjectGroup(session.ProjectId)]
            : [ProjectGroup(session.ProjectId)];
    }

    private sealed record SessionContext(string ProjectId, int? ClassId);
}
