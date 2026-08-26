using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Simulations;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class VirtualLabRuntimeService : IVirtualLabRuntimeService
{
    private const string DefaultBoard = "esp32";
    private const string DefaultLanguage = "arduino";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // Mirrors EducationalEventGenerator.BuildComponentIndexes's own type list
    // EXACTLY (STEM.Application/UseCases/Simulation/Runners/Educational/
    // EducationalEventGenerator.cs) plus the 2 board type strings
    // VirtualLabDiagramService.EnsureBoardPart can emit — this is the
    // definitive, current list of what the Educational runner actually
    // models. Anything not in this set (L298N, DC motor, HC-SR04, DHT11/22,
    // Fan, Drone Motor, line-tracking-3ch/5ch, etc.) is intentionally left to
    // fall through to the existing QEMU-default behavior, unchanged.
    private static readonly HashSet<string> EducationalOnlyComponentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "wokwi-led", "wokwi-pushbutton", "wokwi-buzzer", "wokwi-servo",
        "wokwi-potentiometer", "wokwi-photoresistor-sensor", "wokwi-relay-module",
        "wokwi-pir-motion-sensor", "wokwi-water-leak-sensor", "wokwi-flame-sensor",
        "wokwi-soil-moisture-sensor", "wokwi-rain-sensor", "wokwi-vibration-sensor",
        "wokwi-ir-obstacle-sensor",
        "board-esp32-devkit-c-v4", "wokwi-arduino-uno",
    };

    private readonly StemDbContext _context;
    private readonly VirtualLabDiagramService _diagramService;
    private readonly ISimulationRunnerResolver _runnerResolver;
    private readonly ISimulationCompileService _compileService;
    private readonly IConfiguration _configuration;
    private readonly IRunningSimulationRegistry _runningSimulationRegistry;
    private readonly IPrecompileTriggerService _precompileTrigger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationRepository _notificationRepository;

    public VirtualLabRuntimeService(
        StemDbContext context,
        VirtualLabDiagramService diagramService,
        ISimulationRunnerResolver runnerResolver,
        ISimulationCompileService compileService,
        IConfiguration configuration,
        IRunningSimulationRegistry runningSimulationRegistry,
        IPrecompileTriggerService precompileTrigger,
        IDateTimeProvider dateTimeProvider,
        INotificationRepository notificationRepository)
    {
        _context = context;
        _diagramService = diagramService;
        _runnerResolver = runnerResolver;
        _compileService = compileService;
        _configuration = configuration;
        _runningSimulationRegistry = runningSimulationRegistry;
        _precompileTrigger = precompileTrigger;
        _dateTimeProvider = dateTimeProvider;
        _notificationRepository = notificationRepository;
    }

    public async Task<DiagramSessionResponse?> GetDiagramAsync(
        string sessionId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            return null;
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: true, cancellationToken);
        if (project == null)
        {
            return null;
        }

        var analysis = _diagramService.Analyze(project.DiagramJson, project.Board);
        return BuildDiagramResponse(sessionId, analysis, project.UpdatedAt);
    }

    public async Task<DiagramSessionResponse> SaveDiagramAsync(
        string sessionId,
        SaveDiagramRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            throw new ArgumentException("sessionId must be a GUID virtual-lab project id.");
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: false, cancellationToken);

        if (project == null)
        {
            var platform = await ResolveProjectPlatformAsync(request.LabId, cancellationToken);
            var createAnalysis = _diagramService.Analyze(request.DiagramJson, platform.Board);
            project = new VirtualLabProject
            {
                Id = projectId,
                UserId = currentUserId,
                LabId = request.LabId,
                Name = $"session-{projectId:N}"[..20],
                Board = platform.Board,
                Language = platform.Language,
                DiagramJson = createAnalysis.DiagramJson,
                CodeContent = request.SourceCode ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.VirtualLabProjects.Add(project);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return BuildDiagramResponse(sessionId, createAnalysis, project.UpdatedAt);
            }
            catch (DbUpdateException ex) when (IsDuplicateKey(ex))
            {
                // Race: another request for the same brand-new sessionId (e.g.
                // React StrictMode's double-invoked effect, or a genuine
                // double-submit) already inserted this row first. Fall back to
                // updating the row the other request just created instead of
                // surfacing the raw 500/DB stack trace.
                _context.Entry(project).State = EntityState.Detached;
                project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: false, cancellationToken)
                    ?? throw new InvalidOperationException(
                        "VirtualLabProject not found after a duplicate-key conflict on create.");
            }
        }

        var analysis = _diagramService.Analyze(request.DiagramJson, project.Board);
        project.DiagramJson = analysis.DiagramJson;
        if (request.SourceCode != null)
        {
            project.CodeContent = request.SourceCode;
        }

        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return BuildDiagramResponse(sessionId, analysis, project.UpdatedAt);
    }

    private static bool IsDuplicateKey(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: "23505" };

    public async Task<RunEsp32SimulationResponse> RunEsp32Async(
        RunEsp32SimulationRequest request,
        int? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
            ? Guid.NewGuid().ToString("N")
            : request.SessionId.Trim();
        var mode = ResolveSimulationMode();

        var diagramJson = !string.IsNullOrWhiteSpace(request.DiagramJson)
            ? request.DiagramJson
            : await ResolveDiagramJsonAsync(sessionId, currentUserId, cancellationToken) ?? string.Empty;

        var sourceCode = !string.IsNullOrWhiteSpace(request.SourceCode)
            ? request.SourceCode
            : await ResolveSourceCodeAsync(sessionId, currentUserId, cancellationToken) ?? string.Empty;

        var boardType = await ResolveBoardTypeAsync(sessionId, currentUserId, cancellationToken) ?? DefaultBoard;
        var analysis = _diagramService.Analyze(diagramJson, boardType);

        // CLOSE REMAINING FINAL-LAB GAPS follow-up fix (2026-08-25, found via
        // real manual browser testing, not guessed): ResolveSimulationMode()
        // above is a FLAT global config read (SimulationRunner:DefaultMode) —
        // there is no component-based runtime selection anywhere in this
        // codebase (verified directly in SimulationRunnerResolver.cs), despite
        // a stale comment elsewhere (virtualLabSampleExercises.ts) claiming
        // "kien truc chon runtime dua tren linh kien, khong dua tren nhan bai
        // hoc" — that was never actually implemented. Consequence: with the
        // deployed DefaultMode=qemu, EVERY lab resolves to QemuEsp32Runner,
        // which never references ISimulationInputChannel at all — so live
        // button/potentiometer/light-sensor press-and-hold input silently
        // fails end-to-end ("No running session accepts input for this
        // project right now"), even though EducationalSimulationRunner fully
        // supports it (proven by RealtimeSimulationInputTests.cs) and the
        // component's own runtime models are correct. Minimal, targeted fix:
        // when EVERY component on the diagram is one
        // EducationalEventGenerator.BuildComponentIndexes actually models
        // (plus the board itself), force "educational" regardless of the
        // global default. Any diagram containing L298N/DC-motor/HC-SR04/Fan/
        // DroneMotor/line-tracking-3ch-5ch/etc. falls through untouched to the
        // existing default — this does not change QEMU routing for any lab
        // that genuinely needs it.
        if (mode.Equals("qemu", StringComparison.OrdinalIgnoreCase))
        {
            var snapshot = _diagramService.BuildRuntimeSnapshot(diagramJson, boardType);
            if (snapshot.Components.Count > 0 &&
                snapshot.Components.All(c => EducationalOnlyComponentTypes.Contains(c.Type)))
            {
                mode = "educational";
            }
        }

        // "educational" (EducationalSimulationRunner) và "qemu" (QemuEsp32Runner)
        // không còn tính hết rồi trả về đầy đủ trong 1 request nữa — cả 2 chỉ
        // validate đồng bộ rồi kick off 1 background task (RunAsync trả về gần
        // như ngay lập tức), event thật chảy qua
        // ISimulationEventStore/ISimulationEventBroadcaster TỪ TRONG background
        // task đó, độc lập với request HTTP này. Phải reset SimulationEventsJson="[]"
        // TRƯỚC khi gọi RunAsync (không phải sau) — nếu làm sau, background task
        // có thể đã ghi event đầu tiên trước khi dòng reset chạy tới, và reset sẽ
        // xoá mất nó. Thiếu "qemu" ở đây từng gây race thật: PersistRunAsync (nhánh
        // không-streaming, bên dưới) ghi đè SimulationEventsJson='[]' NGAY SAU KHI
        // RunAsync trả về — chạy sau khi background task đã kịp append vài event
        // đầu sẽ xoá mất chúng. VirtualLabMockRunner (mode "mock") không đổi gì —
        // vẫn đồng bộ, đi qua nhánh PersistRunAsync như cũ 100%.
        var isStreamingMode = mode.Equals("educational", StringComparison.OrdinalIgnoreCase) ||
            mode.Equals("qemu", StringComparison.OrdinalIgnoreCase);
        if (isStreamingMode)
        {
            await PrepareStreamingRunAsync(sessionId, analysis.DiagramJson, sourceCode, request.LabId, currentUserId, cancellationToken);
        }

        var runner = _runnerResolver.Resolve(mode);
        var runResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = sessionId,
            SourceCode = sourceCode,
            DiagramJson = analysis.DiagramJson,
            Mode = mode,
            MaxDurationMs = ReadSimulationRunnerInt("MaxDurationMs", 5000),
            MaxInstructionCount = ReadSimulationRunnerInt("MaxInstructionCount", 10000)
        }, cancellationToken);

        if (isStreamingMode && runResult.Success)
        {
            // Kick-off thành công — KHÔNG gọi PersistRunAsync (nó sẽ ghi đè
            // SimulationEventsJson bằng events=[] của response "started" này,
            // xoá mất bất kỳ event nào background task đã kịp ghi). Status/
            // SimulationEventsJson từ đây do chính background task tự quản.
            return new RunEsp32SimulationResponse
            {
                SessionId = sessionId,
                Status = VirtualLabProjectStatuses.Running,
                Validation = analysis.Validation,
                Netlist = analysis.Netlist,
                Events = Array.Empty<SimulationEventResponse>()
            };
        }

        // Mock runner (luôn đồng bộ) HOẶC streaming mode nhưng validate lỗi
        // ngay từ đầu (diagram/program sai — không có background task nào
        // được kick off, runResult đã là kết quả lỗi đầy đủ) — giữ nguyên
        // hành vi cũ 100%.
        var events = runResult.Events;
        var hasErrors = !analysis.Validation.IsValid ||
            !runResult.Success ||
            events.Any(item => item.Type.Equals("error", StringComparison.OrdinalIgnoreCase));
        var status = hasErrors
            ? VirtualLabProjectStatuses.Error
            : VirtualLabProjectStatuses.Running;

        await PersistRunAsync(
            sessionId,
            analysis.DiagramJson,
            sourceCode,
            status,
            events,
            request.LabId,
            currentUserId,
            cancellationToken);

        return new RunEsp32SimulationResponse
        {
            SessionId = sessionId,
            Status = status,
            Validation = analysis.Validation,
            Netlist = analysis.Netlist,
            Events = events
        };
    }

    // Reset SimulationEventsJson="[]" + Status="running" TRƯỚC khi kick off
    // 1 lần chạy streaming — tạo project mới nếu sessionId chưa từng tồn
    // tại (mirror nhánh "create" của PersistRunAsync, nhưng đơn giản hơn vì
    // không cần atomic-append ở bước này, chỉ 1 lần ghi EF Core bình thường
    // ngay trước khi background task bắt đầu).
    private async Task PrepareStreamingRunAsync(
        string sessionId,
        string diagramJson,
        string sourceCode,
        Guid? labId,
        int? currentUserId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            throw new ArgumentException("sessionId must be a GUID virtual-lab project id.");
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: false, cancellationToken);
        var now = DateTime.UtcNow;

        if (project == null)
        {
            var platform = await ResolveProjectPlatformAsync(labId, cancellationToken);
            project = new VirtualLabProject
            {
                Id = projectId,
                UserId = currentUserId,
                LabId = labId,
                Name = $"session-{projectId:N}"[..20],
                Board = platform.Board,
                Language = platform.Language,
                DiagramJson = diagramJson,
                CodeContent = sourceCode,
                Status = VirtualLabProjectStatuses.Running,
                SimulationEventsJson = "[]",
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.VirtualLabProjects.Add(project);
        }
        else
        {
            project.DiagramJson = diagramJson;
            project.CodeContent = sourceCode;
            project.Status = VirtualLabProjectStatuses.Running;
            project.SimulationEventsJson = "[]";
            project.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> StopSimulationAsync(
        Guid projectId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: false, cancellationToken);
        if (project == null)
        {
            return false;
        }

        // Hủy THẬT lần chạy nền đang diễn ra (nếu có) — trước bản vá này,
        // Stop chỉ đổi Status trong DB, background task (nếu là streaming
        // mode) vẫn tiếp tục chạy vô thời hạn phía sau, không ai biết. Nếu
        // không có gì đang chạy (vd mode "mock", đã hoàn tất từ lâu),
        // TryCancel trả false — vô hại, Status vẫn được set đúng bên dưới.
        _runningSimulationRegistry.TryCancel(projectId.ToString("N"));

        project.Status = VirtualLabProjectStatuses.Stopped;
        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Precompile nền lúc học sinh đang gõ code (FE debounce 2-3s) — mục tiêu:
    // khi học sinh THẬT SỰ bấm Run, firmware cho đúng nội dung mới nhất (nếu
    // họ đã ngừng gõ đủ lâu để debounce bắn ra trước đó) đã sẵn sàng trong
    // IFirmwareCacheService, QemuEsp32Runner rơi vào cache hit thay vì phải
    // compile ~40-90s ngay trong lúc Run. asNoTracking=true — chỉ cần đọc
    // Board/Language để xác định cacheKey, không sửa gì trên project.
    public async Task TriggerPrecompileAsync(
        string sessionId,
        string sourceCode,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            throw new ArgumentException("sessionId must be a GUID virtual-lab project id.");
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: true, cancellationToken)
            ?? throw new KeyNotFoundException("VirtualLabProject not found.");

        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            return;
        }

        _precompileTrigger.TriggerBackgroundCompile(sourceCode, project.Board, project.Language, buildCacheScopeId: projectId);
    }

    public async Task<TeacherProjectSnapshotResponse?> GetProjectSnapshotForTeacherAsync(
        string projectId,
        int teacherId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            return null;
        }

        var project = await _context.VirtualLabProjects
            .AsNoTracking()
            .Where(item => item.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (project == null)
        {
            return null;
        }

        if (!project.LabId.HasValue)
        {
            return null;
        }

        // Cùng điều kiện quyền với VirtualLabHub.EnsureTeacherCanWatchProjectAsync
        // ("Xem live" qua SignalR) — lab phải được gán cho ít nhất 1 lớp giáo
        // viên này dạy. 2 nơi định nghĩa độc lập (khác project/layer, không
        // share code được) — đổi 1 chỗ phải nhớ đổi chỗ kia.
        var canView = await _context.LabClassAssignments
            .AsNoTracking()
            .AnyAsync(
                a => a.LabId == project.LabId.Value && a.Class!.TeacherId == teacherId,
                cancellationToken);

        if (!canView)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem project này.");
        }

        return new TeacherProjectSnapshotResponse
        {
            ProjectId = project.Id.ToString("N"),
            StudentId = project.UserId,
            Board = project.Board,
            Language = project.Language,
            CodeContent = project.CodeContent,
            DiagramJson = project.DiagramJson,
            Status = project.Status,
            UpdatedAt = project.UpdatedAt
        };
    }

    public async Task MarkRunStartedAsync(
        string projectId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new ArgumentException("projectId must be a GUID virtual-lab project id.");
        }

        var project = await LoadOwnedProjectAsync(id, currentUserId, asNoTracking: false, cancellationToken)
            ?? throw new KeyNotFoundException("VirtualLabProject not found.");

        project.Status = VirtualLabProjectStatuses.Running;
        project.SimulationEventsJson = "[]";
        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendSimulationEventAsync(
        string projectId,
        object eventPayload,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(projectId, out var id))
        {
            throw new ArgumentException("projectId must be a GUID virtual-lab project id.");
        }

        _ = await LoadOwnedProjectAsync(id, currentUserId, asNoTracking: true, cancellationToken)
            ?? throw new KeyNotFoundException("VirtualLabProject not found.");

        var eventBatchJson = JsonSerializer.Serialize(new[] { eventPayload }, JsonOptions);
        var affectedRows = await _context.Database.ExecuteSqlRawAsync(
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

        if (affectedRows == 0)
        {
            throw new KeyNotFoundException("VirtualLabProject not found.");
        }
    }

    public async Task<VirtualLabSubmissionResponse> SubmitVirtualLabAsync(
        VirtualLabSubmissionRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.StudentId.HasValue && request.StudentId.Value != currentUserId)
        {
            throw new UnauthorizedAccessException("You cannot submit on behalf of another student.");
        }

        var assignment = await _context.Assignments
            .Include(item => item.SimulationDetail)
            .Include(item => item.Class)
            .FirstOrDefaultAsync(item => item.Id == request.AssignmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Assignment not found.");

        var studentId = currentUserId;

        var existingCount = await _context.Submissions
            .CountAsync(item => item.AssignmentId == assignment.Id && item.StudentId == studentId, cancellationToken);

        // Cộng dồn các ResubmitRequest đã Approved của CHÍNH student này — xem
        // ResubmitEligibility.cs. Không đổi Assignment.DueDate/ResubmitLimit gốc
        // (không ảnh hưởng các học sinh khác).
        var approvedResubmitRequests = await _context.ResubmitRequests
            .AsNoTracking()
            .Where(item =>
                item.AssignmentId == assignment.Id &&
                item.StudentId == studentId &&
                item.Status == ResubmitRequestStatuses.Approved)
            .ToListAsync(cancellationToken);

        if (STEM.Application.UseCases.Grading.ResubmitEligibility.IsBlocked(
                assignment, existingCount, approvedResubmitRequests, _dateTimeProvider.UtcNow, out var blockReason))
        {
            throw new InvalidOperationException(blockReason == "past_due_date"
                ? "Đã quá hạn nộp bài cho assignment này."
                : "Đã đạt giới hạn số lần nộp lại cho assignment này.");
        }

        // Chặn Submit trong lúc simulation vẫn đang chạy nền — SimulationEventsJson
        // đọc được lúc này chỉ là snapshot dở dang (thiếu event chưa kịp ghi),
        // AutoGrade tầng "behavior" sẽ đánh giá sai. Dùng IRunningSimulationRegistry
        // (không phải VirtualLabProject.Status — "Running" trong DB cũng là giá
        // trị của 1 lần chạy đã HOÀN TẤT không lỗi, không chỉ "đang chạy").
        if (!string.IsNullOrWhiteSpace(request.SessionId) && _runningSimulationRegistry.IsRunning(request.SessionId))
        {
            throw new InvalidOperationException(
                "Mô phỏng đang chạy — vui lòng bấm Dừng trước khi nộp bài.");
        }

        var submissionBoardType = await ResolveBoardTypeAsync(request.SessionId, studentId, cancellationToken) ?? DefaultBoard;
        var analysis = _diagramService.Analyze(request.DiagramJson, submissionBoardType);
        var persistedSimulationEvents = await LoadPersistedSimulationEventsAsync(
            request.SessionId,
            studentId,
            cancellationToken);
        var autoCheck = await BuildAutoGradeResultAsync(
            analysis,
            request,
            studentId,
            persistedSimulationEvents,
            cancellationToken);
        var autoScore = assignment.MaxScore * autoCheck.PassedChecks / Math.Max(autoCheck.TotalChecks, 1);

        var contentJson = JsonSerializer.Serialize(new
        {
            virtualLabSubmission = new
            {
                sessionId = request.SessionId,
                diagram = JsonSerializer.Deserialize<JsonElement>(analysis.DiagramJson),
                sourceCode = request.SourceCode,
                compileResult = request.CompileResult,
                simulationSummary = new
                {
                    eventCount = persistedSimulationEvents.Events.Count,
                    events = persistedSimulationEvents.Events
                }
            }
        }, JsonOptions);

        var submission = new Submission
        {
            AssignmentId = assignment.Id,
            StudentId = studentId,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatuses.Submitted,
            ContentJson = contentJson,
            AutoGradeResultJson = JsonSerializer.Serialize(autoCheck, JsonOptions),
            AutoScore = autoScore,
            FinalScore = autoScore,
            AttemptNumber = await GetNextAttemptNumberAsync(assignment.Id, studentId, cancellationToken)
        };

        _context.Submissions.Add(submission);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            // Another request for the same (AssignmentId, StudentId) computed the same
            // AttemptNumber concurrently and won the race. Unlike SaveDiagramAsync, don't
            // silently convert this into an update — a Submission is an append-only
            // attempt log, not a single mutable resource. Reject clearly so the client
            // can resubmit, at which point existingCount/AttemptNumber will be current
            // (and correctly blocked by AllowResubmit/ResubmitLimit if attempts are
            // exhausted).
            throw new InvalidOperationException(
                "Có yêu cầu nộp bài khác đang xử lý cùng lúc, vui lòng thử lại.");
        }

        if (assignment.Class != null)
        {
            var studentName = await _context.Users
                .AsNoTracking()
                .Where(item => item.Id == studentId)
                .Select(item => item.FullName)
                .FirstOrDefaultAsync(cancellationToken) ?? "Học sinh";

            await _notificationRepository.AddAsync(new Notification
            {
                UserId = assignment.Class.TeacherId,
                Title = "Bài nộp mới",
                Content = $"{studentName} đã nộp bài \"{assignment.Title}\".",
                Type = NotificationType.SubmissionReceived
            }, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }

        return new VirtualLabSubmissionResponse
        {
            SubmissionId = submission.Id,
            Status = submission.Status,
            AutoScore = submission.AutoScore,
            AutoCheck = autoCheck
        };
    }

    private async Task PersistRunAsync(
        string sessionId,
        string diagramJson,
        string sourceCode,
        string status,
        IReadOnlyCollection<SimulationEventResponse> events,
        Guid? labId,
        int? currentUserId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            throw new ArgumentException("sessionId must be a GUID virtual-lab project id.");
        }

        var eventBatchJson = JsonSerializer.Serialize(events, JsonOptions);

        // BUG THẬT đã vá (MOTOR ANIMATION VERIFICATION milestone, 2026-08-24,
        // live-verified qua Docker/QEMU thật — không phải suy đoán): StemDbContext
        // dùng NpgsqlRetryingExecutionStrategy (connection resiliency), nhưng
        // transaction thủ công (`BeginTransactionAsync` trần) KHÔNG được phép
        // dùng cùng 1 retrying execution strategy — EF Core chủ động throw
        // "does not support user-initiated transactions" ngay khi strategy cần
        // retry (vd 1 lần transient connection hiccup thật với Supabase pooler ở
        // xa, dễ gặp nhất ở request ĐẦU TIÊN sau khi API mới khởi động — connection
        // pool chưa "ấm"). Hậu quả thật: học sinh bấm Run đúng lúc pod/API vừa
        // restart có thể nhận lỗi 400 khó hiểu thay vì compile lỗi bình thường.
        // Sửa bằng cách bọc TOÀN BỘ thao tác trong `CreateExecutionStrategy()
        // .ExecuteAsync()` — cách EF Core khuyến nghị chính thức để kết hợp
        // transaction thủ công với retrying strategy (tự mở lại transaction MỚI
        // nếu phải retry, thay vì giữ 1 transaction "mồ côi" xung đột với chính
        // sách retry).
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: false, cancellationToken);
            var now = DateTime.UtcNow;
            var appendEventBatch = false;

            if (project == null)
            {
                var platform = await ResolveProjectPlatformAsync(labId, cancellationToken);
                project = new VirtualLabProject
                {
                    Id = projectId,
                    UserId = currentUserId,
                    LabId = labId,
                    Name = $"session-{projectId:N}"[..20],
                    Board = platform.Board,
                    Language = platform.Language,
                    DiagramJson = diagramJson,
                    Status = status,
                    SimulationEventsJson = eventBatchJson,
                    CodeContent = sourceCode,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.VirtualLabProjects.Add(project);
            }
            else
            {
                project.DiagramJson = diagramJson;
                project.CodeContent = sourceCode;
                project.Status = status;
                project.UpdatedAt = now;
                appendEventBatch = true;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (appendEventBatch)
            {
                var affectedRows = await _context.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE "VirtualLabProjects"
                    SET
                        "SimulationEventsJson" = '[]'::jsonb || @eventBatch,
                        "UpdatedAt" = @updatedAt
                    WHERE "Id" = @projectId
                    """,
                    [
                        new NpgsqlParameter("eventBatch", NpgsqlDbType.Jsonb) { Value = eventBatchJson },
                        new NpgsqlParameter("updatedAt", NpgsqlDbType.TimestampTz) { Value = now },
                        new NpgsqlParameter("projectId", NpgsqlDbType.Uuid) { Value = projectId }
                    ],
                    cancellationToken);

                if (affectedRows == 0)
                {
                    throw new KeyNotFoundException("VirtualLabProject not found.");
                }
            }

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task<string?> ResolveDiagramJsonAsync(
        string sessionId,
        int? currentUserId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            return null;
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: true, cancellationToken);
        return project?.DiagramJson;
    }

    private async Task<string?> ResolveSourceCodeAsync(
        string sessionId,
        int? currentUserId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            return null;
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: true, cancellationToken);
        return project?.CodeContent;
    }

    private async Task<string?> ResolveBoardTypeAsync(
        string? sessionId,
        int? currentUserId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out var projectId))
        {
            return null;
        }

        var project = await LoadOwnedProjectAsync(projectId, currentUserId, asNoTracking: true, cancellationToken);
        return project?.Board;
    }

    private async Task<ProjectPlatform> ResolveProjectPlatformAsync(
        Guid? labId,
        CancellationToken cancellationToken)
    {
        if (!labId.HasValue)
        {
            return new ProjectPlatform(DefaultBoard, DefaultLanguage);
        }

        var lab = await _context.Labs
            .AsNoTracking()
            .Where(item => item.Id == labId.Value)
            .Select(item => new { item.BoardType })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Lab not found.");

        // Do NOT NormalizeBoard() here — the resolved value (e.g.
        // "esp32:esp32:esp32:FlashMode=dio") gets stored as VirtualLabProject.Board
        // and later re-used as CompileSimulationRequest.Board, which
        // SimulationCompileService.ValidateRequestAsync checks against SupportedBoards
        // by KEY (raw aliases like "esp32_devkit_v1"), not by the normalized FQBN value.
        // CompileCoreAsync already calls NormalizeBoard exactly once, right before the
        // actual docker/arduino-cli invocation — normalizing here too was a double
        // resolution that made every newly-created project fail to compile/run with
        // "Unsupported board '<already-normalized-fqbn>'".
        return new ProjectPlatform(lab.BoardType, DefaultLanguage);
    }

    /// <summary>
    /// Loads a project by id, or null if it doesn't exist. Throws UnauthorizedAccessException
    /// if the project has a recorded owner and it doesn't match currentUserId — the single
    /// ownership gate shared by every read/write path in this service.
    /// </summary>
    private async Task<VirtualLabProject?> LoadOwnedProjectAsync(
        Guid projectId,
        int? currentUserId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = _context.VirtualLabProjects.Where(item => item.Id == projectId);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var project = await query.FirstOrDefaultAsync(cancellationToken);
        if (project == null)
        {
            return null;
        }

        if (project.UserId.HasValue && project.UserId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not allowed to access this virtual lab project.");
        }

        return project;
    }

    private async Task<AutoGradeResultResponse> BuildAutoGradeResultAsync(
        VirtualLabDiagramAnalysis analysis,
        VirtualLabSubmissionRequest request,
        int studentId,
        PersistedSimulationEventsResult persistedSimulationEvents,
        CancellationToken cancellationToken)
    {
        var checks = new List<AutoGradeCheckResponse>
        {
            new()
            {
                Name = "diagram",
                Passed = analysis.Validation.IsValid,
                Message = analysis.Validation.IsValid
                    ? "Diagram validation passed."
                    : string.Join("; ", analysis.Validation.Errors)
            },
            await BuildCompileCheckAsync(request, studentId, cancellationToken),
            BuildBehaviorCheck(persistedSimulationEvents)
        };

        var passedChecks = checks.Count(item => item.Passed);
        return new AutoGradeResultResponse
        {
            Passed = passedChecks == checks.Count,
            PassedChecks = passedChecks,
            TotalChecks = checks.Count,
            Checks = checks
        };
    }

    private static AutoGradeCheckResponse BuildBehaviorCheck(
        PersistedSimulationEventsResult persistedSimulationEvents)
    {
        if (!string.IsNullOrWhiteSpace(persistedSimulationEvents.FailureMessage))
        {
            return new AutoGradeCheckResponse
            {
                Name = "behavior",
                Passed = false,
                Message = persistedSimulationEvents.FailureMessage
            };
        }

        var hasErrors = persistedSimulationEvents.Events
            .Any(item => item.Type.Equals("error", StringComparison.OrdinalIgnoreCase));

        return new AutoGradeCheckResponse
        {
            Name = "behavior",
            Passed = !hasErrors,
            Message = hasErrors
                ? "Persisted simulation events contain runner errors."
                : "Persisted simulation events were captured."
        };
    }

    private async Task<PersistedSimulationEventsResult> LoadPersistedSimulationEventsAsync(
        string? sessionId,
        int studentId,
        CancellationToken cancellationToken)
    {
        const string missingMessage =
            "No persisted simulation events were found. Run the simulation before submitting.";

        if (!Guid.TryParse(sessionId, out var projectId))
        {
            return new PersistedSimulationEventsResult(Array.Empty<SimulationEventResponse>(), missingMessage);
        }

        var project = await LoadOwnedProjectAsync(projectId, studentId, asNoTracking: true, cancellationToken);
        if (project == null || string.IsNullOrWhiteSpace(project.SimulationEventsJson))
        {
            return new PersistedSimulationEventsResult(Array.Empty<SimulationEventResponse>(), missingMessage);
        }

        try
        {
            var events = JsonSerializer.Deserialize<List<SimulationEventResponse>>(
                project.SimulationEventsJson,
                JsonOptions) ?? new List<SimulationEventResponse>();

            return events.Count == 0
                ? new PersistedSimulationEventsResult(events, missingMessage)
                : new PersistedSimulationEventsResult(events, null);
        }
        catch (JsonException)
        {
            return new PersistedSimulationEventsResult(
                Array.Empty<SimulationEventResponse>(),
                "Persisted simulation events are invalid. Run the simulation again before submitting.");
        }
    }

    /// <summary>
    /// Re-compiles request.SourceCode server-side instead of trusting request.CompileResult
    /// (which is client-supplied and was previously taken on faith — a student could submit
    /// {"compileResult":{"success":true}} for code that never actually compiled). Board and
    /// language come only from the student's own VirtualLabProject record (resolved via
    /// request.SessionId), never from a client-supplied default, so a missing/invalid
    /// SessionId fails this tier outright rather than silently compiling against a guessed
    /// board that could produce a misleading pass/fail.
    /// </summary>
    private async Task<AutoGradeCheckResponse> BuildCompileCheckAsync(
        VirtualLabSubmissionRequest request,
        int studentId,
        CancellationToken cancellationToken)
    {
        const string missingSessionMessage = "Không thể xác định board/ngôn ngữ của bài nộp — thiếu liên kết session hợp lệ.";

        if (!Guid.TryParse(request.SessionId, out var projectId))
        {
            return new AutoGradeCheckResponse { Name = "compile", Passed = false, Message = missingSessionMessage };
        }

        var project = await LoadOwnedProjectAsync(projectId, studentId, asNoTracking: true, cancellationToken);

        if (project == null)
        {
            return new AutoGradeCheckResponse { Name = "compile", Passed = false, Message = missingSessionMessage };
        }

        var compileResult = await _compileService.CompileAsync(new CompileSimulationRequest
        {
            SourceCode = request.SourceCode,
            Board = project.Board,
            Framework = project.Language
        }, studentId, cancellationToken);

        return new AutoGradeCheckResponse
        {
            Name = "compile",
            Passed = compileResult.Success,
            Message = compileResult.Success
                ? "Compile passed."
                : "Compile failed: " + (compileResult.Errors.Count > 0
                    ? string.Join("; ", compileResult.Errors.Select(item => item.Message))
                    : "unknown error.")
        };
    }

    private async Task<int> GetNextAttemptNumberAsync(
        int assignmentId,
        int studentId,
        CancellationToken cancellationToken)
    {
        var latestAttempt = await _context.Submissions
            .AsNoTracking()
            .Where(item => item.AssignmentId == assignmentId && item.StudentId == studentId)
            .Select(item => (int?)item.AttemptNumber)
            .MaxAsync(cancellationToken);

        return (latestAttempt ?? 0) + 1;
    }

    private static DiagramSessionResponse BuildDiagramResponse(
        string sessionId,
        VirtualLabDiagramAnalysis analysis,
        DateTime updatedAt)
    {
        return new DiagramSessionResponse
        {
            SessionId = sessionId,
            DiagramJson = analysis.DiagramJson,
            Validation = analysis.Validation,
            Netlist = analysis.Netlist,
            UpdatedAt = updatedAt
        };
    }

    private string ResolveSimulationMode()
    {
        var configuredMode = _configuration["SimulationRunner:DefaultMode"];
        return string.IsNullOrWhiteSpace(configuredMode)
            ? "mock"
            : configuredMode.Trim().ToLowerInvariant();
    }

    private int ReadSimulationRunnerInt(string key, int fallback)
    {
        return int.TryParse(_configuration[$"SimulationRunner:{key}"], out var value) && value > 0
            ? value
            : fallback;
    }

    private sealed record PersistedSimulationEventsResult(
        IReadOnlyCollection<SimulationEventResponse> Events,
        string? FailureMessage);

    private sealed record ProjectPlatform(string Board, string Language);
}
