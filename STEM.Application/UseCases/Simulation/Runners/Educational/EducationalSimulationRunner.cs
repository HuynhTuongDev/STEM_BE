using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.UseCases.Simulation.Runners.Educational;

public sealed class EducationalSimulationRunner : ISimulationRunner
{
    private const int DefaultMaxConcurrentRuns = 10;

    private readonly VirtualLabDiagramService _diagramService;
    private readonly EducationalProgramAnalyzer _analyzer;
    private readonly EducationalEventGenerator _eventGenerator;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISimulationEventBroadcaster _broadcaster;
    private readonly IRunningSimulationRegistry _registry;
    private readonly ISimulationInputChannel _inputChannel;
    private readonly SemaphoreSlim _runGate;

    public EducationalSimulationRunner(
        VirtualLabDiagramService diagramService,
        EducationalProgramAnalyzer analyzer,
        EducationalEventGenerator eventGenerator,
        IServiceScopeFactory scopeFactory,
        ISimulationEventBroadcaster broadcaster,
        IRunningSimulationRegistry registry,
        ISimulationInputChannel inputChannel,
        IConfiguration configuration)
    {
        _diagramService = diagramService;
        _analyzer = analyzer;
        _eventGenerator = eventGenerator;
        _scopeFactory = scopeFactory;
        _broadcaster = broadcaster;
        _registry = registry;
        _inputChannel = inputChannel;

        var maxConcurrentRuns = int.TryParse(
            configuration["SimulationRunner:MaxConcurrentRuns"],
            out var configuredMaxConcurrentRuns)
                ? configuredMaxConcurrentRuns
                : DefaultMaxConcurrentRuns;
        _runGate = new SemaphoreSlim(Math.Max(1, maxConcurrentRuns));
    }

    // Model streaming: validate diagram+program ĐỒNG BỘ (nhanh, không delay
    // thật nào) — nếu sai, trả lỗi ngay như trước. Nếu hợp lệ, đăng ký 1
    // CancellationTokenSource ĐỘC LẬP (không link với `cancellationToken`
    // của request — HttpContext có thể bị dispose/pool lại ngay sau khi
    // response trả về, tiếp tục dùng token dẫn xuất từ nó là không an toàn)
    // vào IRunningSimulationRegistry rồi kick off Task.Run KHÔNG await —
    // trả về ngay 1 "started" stub. Toàn bộ event thật chảy qua
    // ISimulationEventStore (ghi DB)/ISimulationEventBroadcaster (đẩy Hub)
    // trong lúc Task.Run chạy nền.
    public async Task<SimulationRunResult> RunAsync(
        SimulationRunContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var diagramAnalysis = _diagramService.Analyze(context.DiagramJson);
        var program = _analyzer.Analyze(context.SourceCode);
        var errors = new List<string>();
        var warnings = new List<string>();

        if (!diagramAnalysis.Validation.IsValid)
        {
            errors.AddRange(diagramAnalysis.Validation.Errors);
        }

        errors.AddRange(program.Errors);
        warnings.AddRange(diagramAnalysis.Validation.Warnings);
        warnings.AddRange(program.Warnings);

        if (errors.Count > 0)
        {
            return new SimulationRunResult
            {
                Success = false,
                Events = errors.Select(error => new SimulationEventResponse
                {
                    Type = "error",
                    Time = 0,
                    Payload = new Dictionary<string, object?>
                    {
                        ["message"] = error
                    }
                }).ToList(),
                Errors = errors,
                Warnings = warnings
            };
        }

        var snapshot = _diagramService.BuildRuntimeSnapshot(diagramAnalysis.DiagramJson);

        var runCts = new CancellationTokenSource();
        _registry.Register(context.ProjectId, runCts);
        // Same instance the background loop reads from (state.Context.ComponentInputs
        // in EducationalEventGenerator) — a live SetSimulationInput call reaching
        // this dictionary is picked up on the NEXT digitalRead(), no restart needed.
        _inputChannel.RegisterSession(context.ProjectId, context.ComponentInputs);

        _ = Task.Run(
            () => ExecuteInBackgroundAsync(program, snapshot, context, runCts),
            CancellationToken.None);

        return new SimulationRunResult
        {
            Success = true,
            Events = Array.Empty<SimulationEventResponse>(),
            Errors = Array.Empty<string>(),
            Warnings = warnings
        };
    }

    private async Task ExecuteInBackgroundAsync(
        EducationalProgram program,
        VirtualLabRuntimeDiagramSnapshot snapshot,
        SimulationRunContext context,
        CancellationTokenSource runCts)
    {
        // Scope riêng cho LẦN CHẠY NÀY — không tái dùng scope của request HTTP
        // đã kết thúc từ lâu (StemDbContext trong đó có thể đã bị dispose).
        using var scope = _scopeFactory.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<ISimulationEventStore>();
        var cancellationToken = runCts.Token;

        // Khởi tạo sẵn 1 giá trị an toàn — finally bên dưới vẫn chạy được
        // dù try/catch không gán lại (vd exception thoát khỏi catch(Exception)
        // vì lý do gì đó), tránh lỗi "unassigned local variable".
        var finalStatus = VirtualLabProjectStatuses.Error;
        string? reason = "unknown error";

        try
        {
            await _runGate.WaitAsync(cancellationToken);
            try
            {
                async Task OnEventEmittedAsync(SimulationEventResponse evt)
                {
                    // Ghi DB trước, broadcast sau — nếu client đang xem
                    // realtime bắt được event sớm hơn 1 nhịp so với khi nó
                    // thực sự đã nằm trong DB thì vô hại (broadcast chỉ để
                    // hiển thị ngay, DB mới là nguồn tin cậy cho AutoGrade —
                    // xem BuildAutoGradeResultAsync), nhưng ĐỌC lại
                    // SimulationEventsJson trước khi ghi nó xong thì có thể
                    // thiếu — thứ tự ngược lại rủi ro hơn.
                    await eventStore.AppendEventAsync(context.ProjectId, evt, cancellationToken);
                    await _broadcaster.BroadcastEventAsync(context.ProjectId, evt, cancellationToken);
                }

                var result = await _eventGenerator.GenerateAsync(
                    program, snapshot, context, OnEventEmittedAsync, cancellationToken);

                finalStatus = result.Success ? VirtualLabProjectStatuses.Running : VirtualLabProjectStatuses.Error;
                reason = result.Success ? "completed" : string.Join("; ", result.Errors);
            }
            finally
            {
                _runGate.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Hoặc do Stop (IRunningSimulationRegistry.TryCancel) hoặc do
            // _runGate hết chỗ quá lâu — cả 2 đều coi là "đã dừng", không
            // phải lỗi hệ thống.
            finalStatus = VirtualLabProjectStatuses.Stopped;
            reason = "stopped";
        }
        catch (Exception ex)
        {
            finalStatus = VirtualLabProjectStatuses.Error;
            reason = ex.Message;
        }
        finally
        {
            // Remove TRƯỚC khi ghi/notify — nếu học sinh bấm Run lại ngay
            // trong lúc đang dọn dẹp, Register() của lần chạy mới không bị
            // hiểu nhầm là "phải hủy chính nó".
            _registry.Remove(context.ProjectId);
            _inputChannel.UnregisterSession(context.ProjectId);

            try
            {
                await eventStore.MarkRunFinishedAsync(context.ProjectId, finalStatus, CancellationToken.None);
                await _broadcaster.BroadcastRunCompletedAsync(context.ProjectId, finalStatus, reason, CancellationToken.None);
            }
            catch
            {
                // Fire-and-forget task — không còn ai để throw tới nữa.
                // Best-effort: nếu DB/Hub tạm thời lỗi ở đúng lúc dọn dẹp,
                // không có gì để làm thêm ở đây.
            }
        }
    }
}
