using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runners.Educational.Components;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.UseCases.Simulation.Runners.Qemu;

// Engine mô phỏng chạy firmware ESP32 THẬT (đã compile qua ISimulationCompileService/
// Docker sandbox có sẵn) trong QEMU, thay vì diễn giải lại source code bằng interpreter
// tự viết (EducationalSimulationRunner). Vì firmware đã đúng 100% theo compiler thật,
// QEMU tự xử lý đúng MỌI cấu trúc điều khiển (for/while/if/hàm lồng nhau...) — loại bỏ
// hoàn toàn rủi ro "interpreter hiểu sai ngữ nghĩa" đã gặp phải (xem
// EducationalRunner_ForLoopWithTrailingStatement_ProducesCorrectSequence, còn FAIL vì
// gap for/while chưa implement).
//
// Feasibility đã verify thật (không suy đoán): firmware Arduino-ESP32 build với
// FlashMode=dio boot sạch trong qemu-system-xtensa (Espressif official release), stdio
// đọc được, delay() đo được đúng CHÍNH XÁC 1.000s thời gian thực giữa các tick. Verify
// lại lần 2 (sau khi nối vào pipeline streaming thật): dùng merged.bin thật lấy qua API
// (không phải build tay) xác nhận for/while được QEMU thực thi ĐÚNG 100% ngữ nghĩa C++
// thật — 3 lần nháy rồi im lặng vĩnh viễn, KHÔNG còn lệch nhịp như interpreter cũ.
//
// GPIO đọc qua kỹ thuật tiêm macro #define digitalWrite — đã trải qua 2 vòng vá bug
// thật trong lúc verify:
// (1) Bản đầu dùng Serial.println() (C++ HardwareSerial) trong wrapper, có IRAM_ATTR —
//     TƯỞNG đã hết crash nhưng verify sâu hơn phát hiện vẫn "Guru Meditation Error:
//     Cache error" (EXCVADDR=0x0) một cách KHÔNG TẤT ĐỊNH — cùng 1 source code compile
//     2 lần cho ra firmware lúc chạy sạch, lúc crash (đã diff xác nhận source giống hệt
//     nhau byte-for-byte). Không phải do thiếu Serial.begin() (đã thử thêm đúng chỗ,
//     vẫn crash) — nghi ngờ cao là race condition thật trong driver UART/interrupt của
//     ESP32 Arduino core khi chạy dưới QEMU, không phải bug ở phía code này.
// (2) Sửa triệt để bằng cách BỎ HẲN Serial/HardwareSerial — in qua `ets_printf` (hàm
//     UART cấp thấp nằm trong ROM, KHÔNG qua driver C++ HardwareSerial, không cần
//     Serial.begin(), không có gì để race). Đây chính là hàm mà bootloader/ROM dùng để
//     in log "ets Jul 29 2019..." mà mọi lần boot đều thấy in sạch — chưa từng crash
//     dù chỉ 1 lần. Đã verify: 3 lần compile độc lập + 5 lần boot, TẤT CẢ đều sạch, kể
//     cả khi sketch học sinh không hề gọi Serial.begin(). IRAM_ATTR trên wrapper vẫn
//     giữ (không còn bắt buộc để hết crash, nhưng vẫn đúng nguyên tắc — hàm được gọi từ
//     mọi nơi trong code người dùng, không nên phụ thuộc trạng thái cache).
//
// QUAN TRỌNG: khác EducationalSimulationRunner (validate diagram/program gần như tức
// thời), bước "validate" thật sự của runner này là COMPILE — đo thật mất ~37s (Docker
// sandbox arduino-cli). KHÔNG được compile trong phần đồng bộ của RunAsync (sẽ phá vỡ
// hợp đồng "trả về gần như ngay lập tức" của toàn bộ kiến trúc streaming) — compile bị
// đẩy hẳn vào trong background task, cùng chỗ với QEMU. Hệ quả: lỗi compile chỉ được
// biết qua StudentRunCompleted (status=error), không phải qua response /start đồng bộ
// như lỗi diagram — khác EducationalSimulationRunner, chấp nhận được vì bản chất khác
// nhau (validate tức thời vs validate tốn ~37s).
public sealed class QemuEsp32Runner : ISimulationRunner
{
    private const int DefaultMaxConcurrentRuns = 4;

    private readonly VirtualLabDiagramService _diagramService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISimulationEventBroadcaster _broadcaster;
    private readonly IRunningSimulationRegistry _registry;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _runGate;

    public QemuEsp32Runner(
        VirtualLabDiagramService diagramService,
        IServiceScopeFactory scopeFactory,
        ISimulationEventBroadcaster broadcaster,
        IRunningSimulationRegistry registry,
        IConfiguration configuration)
    {
        _diagramService = diagramService;
        _scopeFactory = scopeFactory;
        _broadcaster = broadcaster;
        _registry = registry;
        _configuration = configuration;

        var maxConcurrentRuns = int.TryParse(
            configuration["SimulationRunner:Qemu:MaxConcurrentRuns"],
            out var configured)
                ? configured
                : DefaultMaxConcurrentRuns;
        _runGate = new SemaphoreSlim(Math.Max(1, maxConcurrentRuns));
    }

    public async Task<SimulationRunResult> RunAsync(
        SimulationRunContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Chỉ những kiểm tra RẺ (tức thời) mới nằm trong phần đồng bộ — compile (tốn
        // ~37s) bị đẩy hẳn vào background task bên dưới.
        var diagramAnalysis = _diagramService.Analyze(context.DiagramJson);
        if (!diagramAnalysis.Validation.IsValid)
        {
            return SimulationRunResult.Failed(diagramAnalysis.Validation.Errors.ToArray());
        }

        var board = ExtractBoard(diagramAnalysis.DiagramJson);
        if (IsAvrBoard(board))
        {
            return SimulationRunResult.Failed(
                $"QEMU runner chỉ hỗ trợ board ESP32 (Xtensa) — board '{board}' là AVR, không boot được trong qemu-system-xtensa.");
        }

        var snapshot = _diagramService.BuildRuntimeSnapshot(diagramAnalysis.DiagramJson);

        // Sensor Input Bridge — Phase 1 (scenario/timeline, xem
        // SensorRuntimeHeaderGenerator.cs + VIRTUAL_LAB_PLAN.md). Tắt theo mặc
        // định (feature flag rỗng/false) — sensorHeader rỗng "" thì mọi hành vi
        // compile/cache giữ NGUYÊN như trước khi có tính năng này (xem
        // FirmwareCacheService.ResolveCacheDir). Tính rẻ (parse JSON string,
        // không I/O) — an toàn để làm trong phần đồng bộ của RunAsync.
        var sensorHeader = string.Empty;
        IReadOnlyDictionary<string, string>? extraFiles = null;
        if (_configuration.GetValue("SimulationRunner:Qemu:EnableSensorInputScenario", false))
        {
            var scenario = SensorRuntimeHeaderGenerator.TryParseScenario(diagramAnalysis.DiagramJson);
            sensorHeader = SensorRuntimeHeaderGenerator.Generate(snapshot, scenario) ?? string.Empty;
            // DHT fix (2026-08-23): "#include \"StemFlowDHT.h\"" trong sketch học sinh
            // (StemFlowDHT.h) cần file thật tồn tại để preprocessor resolve, dù class
            // StemFlowDHT thật đã được Generate() nhúng sẵn ở trên — xem
            // SensorRuntimeHeaderGenerator.BuildExtraFiles.
            extraFiles = SensorRuntimeHeaderGenerator.BuildExtraFiles(snapshot);
        }

        // WiFi/Cloud — Virtual Cloud Runtime Phase 1 (xem
        // CloudRuntimeHeaderGenerator.cs + VIRTUAL_LAB_PLAN.md). CỘNG THÊM vào
        // sensorHeader (không phải khối riêng) — ResolveCacheDir chỉ nhận 1
        // tham số sensorHeader, cộng chuỗi ở đây là đủ, không cần đổi chữ ký
        // FirmwareCacheService. Rỗng khi diagram không có Cloud/Dashboard
        // component -> cache key không đổi cho mọi diagram cũ.
        if (_configuration.GetValue("SimulationRunner:Qemu:EnableCloudRuntime", false))
        {
            sensorHeader += CloudRuntimeHeaderGenerator.Generate(diagramAnalysis.DiagramJson) ?? string.Empty;
        }

        var runCts = new CancellationTokenSource();
        _registry.Register(context.ProjectId, runCts);

        _ = Task.Run(
            () => ExecuteInBackgroundAsync(context.SourceCode, board, snapshot, context, runCts, sensorHeader, extraFiles),
            CancellationToken.None);

        return new SimulationRunResult
        {
            Success = true,
            Events = Array.Empty<SimulationEventResponse>(),
            Errors = Array.Empty<string>(),
            Warnings = diagramAnalysis.Validation.Warnings
        };
    }

    private async Task ExecuteInBackgroundAsync(
        string sourceCode,
        string board,
        VirtualLabRuntimeDiagramSnapshot snapshot,
        SimulationRunContext context,
        CancellationTokenSource runCts,
        string sensorHeader,
        IReadOnlyDictionary<string, string>? extraFiles)
    {
        // Scope riêng cho LẦN CHẠY NÀY — IFirmwareCacheService (phụ thuộc
        // ISimulationCompileService, Scoped vì phụ thuộc StemDbContext) KHÔNG được
        // inject thẳng vào constructor (QemuEsp32Runner là Singleton, giữ dependency
        // Scoped trực tiếp là captive-dependency, sai vòng đời) — resolve fresh từ
        // scope này, cùng cách đã làm với ISimulationEventStore.
        using var scope = _scopeFactory.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<ISimulationEventStore>();
        var firmwareCache = scope.ServiceProvider.GetRequiredService<IFirmwareCacheService>();
        var cancellationToken = runCts.Token;
        var componentIndex = new ComponentIndex(snapshot);
        var containerName = $"stem-qemu-{Guid.NewGuid():N}";

        var finalStatus = VirtualLabProjectStatuses.Error;
        string? reason = "unknown error";
        Process? process = null;
        string? jobRoot = null;

        try
        {
            // Firmware cache theo NỘI DUNG (sourceCode+board+framework+fqbn+
            // instrumentationVersion) — nếu trùng y hệt lần compile trước (của
            // project này, project khác, hoặc bản precompile sẵn cho starterCode
            // của Lab lúc giáo viên lưu bài), bỏ qua HẲN việc compile, chạy QEMU
            // ngay — đúng mục tiêu "nhanh như Wokwi" cho bài mẫu chưa sửa gì.
            var cachedFirmware = await firmwareCache.TryGetCachedFirmwareAsync(sourceCode, board, "arduino", cancellationToken, sensorHeader);
            CompileSimulationResponse compileResult;
            if (cachedFirmware != null)
            {
                compileResult = cachedFirmware;
                // Cache hit — KHÔNG phát StudentCompileStarted/Finished, vì không
                // hề có bước compile nào xảy ra ở lần chạy này (FE nhảy thẳng từ
                // "Đang kiểm tra sơ đồ mạch..." sang "Đang khởi động mô phỏng...").
            }
            else
            {
                // Giai đoạn compile (~40-90s thật, đo qua verify) dùng thẳng
                // `cancellationToken` (chỉ hủy khi Stop thật hoặc lỗi hệ thống) — KHÔNG
                // tính vào ngân sách context.MaxDurationMs. SimulationCompileService tự
                // có timeout nội bộ riêng (SimulationCompile:TimeoutSeconds) rồi.
                // VIỆC A (UX loading theo giai đoạn): báo ngay trước khi compile thật bắt
                // đầu — đây là giai đoạn DÀI NHẤT (~40-90s, đo thật ở VIỆC B), FE cần biết
                // để hiện đúng "Đang biên dịch code..." thay vì 1 spinner mơ hồ. Tái dùng
                // đúng tên "StudentCompileStarted" mà ClassMonitorPage.tsx (Dashboard giáo
                // viên) đã lắng nghe sẵn từ trước.
                await _broadcaster.BroadcastCompileStartedAsync(context.ProjectId, cancellationToken);

                // buildCacheScopeId (cache build-path VIỆC B, khác cache firmware theo
                // nội dung ở trên) — AN TOÀN dùng projectId ở đây vì
                // IRunningSimulationRegistry đảm bảo tại 1 thời điểm chỉ có tối đa 1
                // background task (và do đó 1 compile) cho mỗi projectId — không bao giờ
                // bị 2 compile đồng thời cùng ghi 1 build-path (đã verify thật: ghi đồng
                // thời gây lẫn dữ liệu compile).
                var buildCacheScopeId = Guid.TryParse(context.ProjectId, out var parsedScopeId)
                    ? parsedScopeId
                    : (Guid?)null;
                compileResult = await firmwareCache.CompileAndCacheAsync(sourceCode, board, "arduino", buildCacheScopeId, cancellationToken, sensorHeader, extraFiles);

                if (!compileResult.Success || string.IsNullOrEmpty(compileResult.FirmwareBase64))
                {
                    finalStatus = VirtualLabProjectStatuses.Error;
                    reason = compileResult.Errors.Count > 0
                        ? string.Join("; ", compileResult.Errors.Select(e => e.Line.HasValue ? $"Dòng {e.Line}: {e.Message}" : e.Message))
                        : "Compile thất bại, không có firmware để chạy QEMU.";
                    await _broadcaster.BroadcastCompileFinishedAsync(context.ProjectId, success: false, reason, cancellationToken);
                    return;
                }

                await _broadcaster.BroadcastCompileFinishedAsync(context.ProjectId, success: true, errorSummary: null, cancellationToken);
            }

            // Phòng thủ: cache hỏng (đọc được meta nhưng firmware base64 rỗng) hoặc
            // 1 nhánh nào đó phía trên trả compileResult thiếu firmware — không nên
            // xảy ra theo logic hiện tại, nhưng chặn ở đây rẻ hơn NRE khi ghi file.
            if (string.IsNullOrEmpty(compileResult.FirmwareBase64))
            {
                finalStatus = VirtualLabProjectStatuses.Error;
                reason = "Không có firmware để chạy QEMU (cache hoặc compile result rỗng).";
                return;
            }

            jobRoot = Path.Combine(ResolveWorkingRoot(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(jobRoot);
            var firmwarePath = Path.Combine(jobRoot, "firmware.bin");
            await File.WriteAllBytesAsync(firmwarePath, Convert.FromBase64String(compileResult.FirmwareBase64), cancellationToken);

            // Log dir bind-mount RIÊNG (không phải stdout pipe) — xem lý do trong
            // comment ở StartQemuProcess/vòng lặp đọc log bên dưới.
            var logDir = Path.Combine(jobRoot, "log");
            Directory.CreateDirectory(logDir);
            var logFilePath = Path.Combine(logDir, "serial.log");

            // Ngân sách context.MaxDurationMs chỉ bắt đầu tính từ ĐÂY — lúc QEMU thật
            // sự chạy mô phỏng — không tính thời gian compile phía trên vào (khớp
            // đúng kỳ vọng FE/Stop, cùng ý nghĩa MaxDurationMs như
            // EducationalSimulationRunner). Lưới an toàn CHỦ ĐỘNG — không dựa vào
            // OOM làm cơ chế chính (đã đo thật ở 3 test độc hại: firmware spam
            // Serial mất tới ~20s mới tự OOM-kill, quá chậm để làm cơ chế dừng
            // chính, chỉ nên là lưới dự phòng thứ 2 giống compile sandbox).
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Math.Max(1, context.MaxDurationMs));

            await _runGate.WaitAsync(timeoutCts.Token);
            try
            {
                // Phát SAU khi qua được _runGate (không phải trước) — nếu đang xếp
                // hàng chờ (MaxConcurrentRuns đầy), học sinh phải vẫn thấy "Đang biên
                // dịch..."/trạng thái trước đó, không nhảy sớm sang "Đang khởi động
                // mô phỏng..." trong lúc thực ra vẫn đang chờ tới lượt.
                await _broadcaster.BroadcastRunBootingAsync(context.ProjectId, cancellationToken);

                // Retry NGẦM tối đa 2 lần (3 lần thử tổng cộng) khi phát hiện "process
                // tự thoát sớm" — xem giải thích đầy đủ ở comment trên
                // ReadNewLogLinesAsync/8.13. Tín hiệu đáng ngờ là process.HasExited trở
                // thành true KHÔNG PHẢI do timeoutCts (MaxDurationMs/Stop) mà tự nhiên
                // (QEMU với -no-reboot + sketch loop() vô hạn KHÔNG BAO GIỜ tự thoát hợp
                // lệ) — chỉ retry khi CHƯA emit event nào ở lần thử trước (an toàn:
                // không có dữ liệu đã ghi vào eventStore cần dọn/lo trùng lặp; nếu đã có
                // event mà vẫn thoát sớm thì giữ nguyên kết quả, không retry, tránh trộn
                // lẫn 2 lần chạy khác container/timestamp vào cùng 1 project). 3 lần thử
                // (không phải 2) vì đã đo thật: 2 lần liên tiếp CÙNG fail xảy ra thật (dù
                // hiếm) — xem VIRTUAL_LAB_PLAN.md 8.13.
                const int maxAttempts = 3;
                var attempt = 0;
                while (true)
                {
                    attempt++;
                    if (attempt > 1)
                    {
                        containerName = $"stem-qemu-{Guid.NewGuid():N}";
                    }

                    process = StartQemuProcess(firmwarePath, logDir, containerName);
                    process.Start();
                    var stdoutTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
                    var stderrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
                    var stopwatch = Stopwatch.StartNew();

                    // Đọc sự kiện GPIO qua FILE (-serial file:..., bind-mount RIÊNG,
                    // KHÔNG qua stdout pipe của chính process) — QEMU `-nographic`
                    // (multiplex serial+monitor chung 1 stdio, cần tty raw mode) tự
                    // thoát sạch dưới pipe của .NET Process — `-serial file:` tránh
                    // hoàn toàn vấn đề tty/pipe này. Xem VIRTUAL_LAB_PLAN.md 8.13.
                    long logOffset = 0;
                    var pendingLine = new System.Text.StringBuilder();
                    var eventsEmitted = 0;
                    while (!process.HasExited)
                    {
                        timeoutCts.Token.ThrowIfCancellationRequested();
                        var (newOffset, emittedThisPoll) = await ReadNewLogLinesAsync(
                            logFilePath, logOffset, pendingLine, componentIndex, eventStore, _broadcaster, context, stopwatch, cancellationToken);
                        logOffset = newOffset;
                        eventsEmitted += emittedThisPoll;
                        await Task.Delay(150, timeoutCts.Token);
                    }

                    // Đọc nốt phần còn lại sau khi process đã thoát (dữ liệu ghi ra
                    // file ngay trước lúc thoát có thể chưa kịp đọc ở lần poll cuối).
                    var (_, emittedAfterExit) = await ReadNewLogLinesAsync(
                        logFilePath, logOffset, pendingLine, componentIndex, eventStore, _broadcaster, context, stopwatch, cancellationToken);
                    eventsEmitted += emittedAfterExit;

                    if (Environment.GetEnvironmentVariable("STEM_QEMU_DEBUG") == "1")
                    {
                        var stdoutText = await stdoutTask;
                        var stderrText = await stderrTask;
                        Console.Error.WriteLine($"=== QEMU container stdout (attempt {attempt}) ===");
                        Console.Error.WriteLine(stdoutText);
                        Console.Error.WriteLine($"=== QEMU container stderr (attempt {attempt}) ===");
                        Console.Error.WriteLine(stderrText);
                        Console.Error.WriteLine($"=== QEMU process ExitCode={process.ExitCode} (attempt {attempt}) ===");
                    }

                    var suspiciousEarlyExit = eventsEmitted == 0
                        && stopwatch.ElapsedMilliseconds < context.MaxDurationMs * 0.7;

                    if (!suspiciousEarlyExit || attempt >= maxAttempts)
                    {
                        break;
                    }

                    // Dọn container lần thử vừa rồi trước khi thử lại — không đợi
                    // tới finally cuối cùng (finally chỉ dọn container CUỐI CÙNG).
                    await TryDockerRemoveAsync(containerName);
                    process.Dispose();
                    process = null;
                }

                finalStatus = VirtualLabProjectStatuses.Running;
                reason = "completed";
            }
            finally
            {
                _runGate.Release();
            }
        }
        catch (OperationCanceledException)
        {
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
            // KHÔNG dùng "--rm" trong StartQemuProcess (giống SimulationCompileService,
            // xem comment ở đó) — xác nhận thật: trên Windows, `docker run --rm` chạy
            // foreground qua .NET Process (RedirectStandardOutput/Error, có hoặc không
            // CreateNoWindow) có race khiến CLIENT `docker.exe` tự thoát sớm
            // (ExitCode=0, tưởng như container đã xong) trong khi *container thật* vẫn
            // đang chạy dở trong daemon — QEMU chỉ boot xong + in được 0-1 sự kiện đầu
            // trước khi mất kết nối. Bỏ "--rm", dọn tường minh bằng "docker rm -f" ở
            // đây (không phải "docker kill" — "rm -f" vừa kill vừa xoá container luôn
            // trong 1 lệnh, cần thiết vì giờ container không tự xoá) đã verify hết hẳn
            // race này — chạy lặp lại nhiều lần cho sự kiện đầy đủ, đúng nhịp thời
            // gian thật. Xem VIRTUAL_LAB_PLAN.md mục 8.13.
            await TryDockerRemoveAsync(containerName);
            TryKillProcess(process);
            process?.Dispose();
            if (jobRoot != null && Environment.GetEnvironmentVariable("STEM_QEMU_DEBUG") != "1")
            {
                TryDeleteDirectory(jobRoot);
            }
            else if (jobRoot != null)
            {
                Console.Error.WriteLine($"=== jobRoot kept for debug: {jobRoot} ===");
            }

            _registry.Remove(context.ProjectId);

            try
            {
                await eventStore.MarkRunFinishedAsync(context.ProjectId, finalStatus, CancellationToken.None);
                await _broadcaster.BroadcastRunCompletedAsync(context.ProjectId, finalStatus, reason, CancellationToken.None);
            }
            catch
            {
                // Fire-and-forget task — không còn ai để throw tới nữa.
            }
        }
    }

    // BUG THẬT vừa vá (2026-07-26): thiếu hẳn lời gọi BroadcastEventAsync —
    // trước đây chỉ ghi vào eventStore (DB), KHÔNG BAO GIỜ phát
    // "StudentSimulationEvent" qua SignalR. Xác nhận qua DB thật: project vừa
    // chạy có SimulationEventsJson đầy đủ (backend/QEMU chạy đúng), nhưng FE
    // Network tab (WebSocket) chỉ thấy {"type":6} (ping keepalive) — không hề
    // có StudentSimulationEvent nào tới, dù JoinSession/group đã đúng. Đúng
    // pattern EducationalSimulationRunner đã dùng từ trước (AppendEventAsync
    // + BroadcastEventAsync đi CÙNG NHAU, không phải chỉ 1 trong 2) — xem
    // VIRTUAL_LAB_PLAN.md 8.14.
    private static async Task EmitAsync(
        ISimulationEventStore eventStore,
        ISimulationEventBroadcaster broadcaster,
        string projectId,
        SimulationEventResponse evt,
        CancellationToken cancellationToken)
    {
        await eventStore.AppendEventAsync(projectId, evt, cancellationToken);
        await broadcaster.BroadcastEventAsync(projectId, evt, cancellationToken);
    }

    // Đọc phần MỚI xuất hiện trong serial.log kể từ `offset`, tách dòng hoàn
    // chỉnh (kết thúc bằng '\n') để parse SF_EVENT, giữ lại phần dòng dở dang
    // cuối cùng trong `pendingLine` cho lần poll sau (tránh cắt đôi 1 dòng
    // đang được QEMU ghi dở). File có thể CHƯA TỒN TẠI ở vài lần poll đầu
    // (QEMU cần 1 khoảng ngắn để tạo file log) — coi như chưa có gì mới, không
    // phải lỗi. FileShare.ReadWrite bắt buộc vì QEMU đang giữ file mở để ghi
    // liên tục trong lúc ta đọc.
    // Trả về (offset mới, số event VỪA emit trong lần gọi này) — không dùng
    // "ref int" vì tham số ref/out không hợp lệ trong async method; caller tự
    // cộng dồn vào tổng của mình để phát hiện "0 event toàn bộ attempt" (tín
    // hiệu retry, xem 8.13).
    private static async Task<(long Offset, int EventsEmitted)> ReadNewLogLinesAsync(
        string logFilePath,
        long offset,
        System.Text.StringBuilder pendingLine,
        ComponentIndex componentIndex,
        ISimulationEventStore eventStore,
        ISimulationEventBroadcaster broadcaster,
        SimulationRunContext context,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(logFilePath))
        {
            return (offset, 0);
        }

        string chunk;
        long newOffset;
        try
        {
            using var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (stream.Length <= offset)
            {
                return (offset, 0);
            }

            stream.Seek(offset, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
            chunk = await reader.ReadToEndAsync(cancellationToken);
            newOffset = stream.Position;
        }
        catch (IOException)
        {
            // File đang được QEMU ghi dở, gặp lock thoáng qua — bỏ qua lần poll
            // này, thử lại ở lần sau (150ms), không phải lỗi thật.
            return (offset, 0);
        }

        pendingLine.Append(chunk);
        var combined = pendingLine.ToString();
        var lines = combined.Split('\n');
        pendingLine.Clear();
        pendingLine.Append(lines[^1]); // phần dòng dở dang cuối cùng, giữ lại

        var eventsEmitted = 0;
        for (var i = 0; i < lines.Length - 1; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (Environment.GetEnvironmentVariable("STEM_QEMU_DEBUG") == "1")
            {
                Console.Error.WriteLine($"[serial.log] {line}");
            }

            // WiFi/Cloud Phase 1 — kiểm tra marker cloud TRƯỚC SF_EVENT (không đụng
            // nhau: "SF_CLOUD_EVENT"/"SF_CLOUD_LOG" không chứa substring "SF_EVENT "
            // nên thứ tự không quan trọng, nhưng để rõ ràng vẫn xử lý riêng ở đây).
            // KHÔNG increment eventsEmitted (giống nhánh raw-serial-passthrough bên
            // dưới) — cloud event không ảnh hưởng heuristic suspiciousEarlyExit.
            // VIRTUAL LAB RUNTIME CAPABILITY EXPANSION (STEP 3, 2026-08-26) —
            // servoId already IS the diagram componentId (StemFlowPCA9685's
            // caller passes it directly, same "match by componentId string, not
            // physical pin" convention already established for StemFlowDHT) —
            // no ComponentIndex/netlist lookup needed, unlike LED/Buzzer/Fan
            // which resolve via GPIO pin. Checked before the generic SF_EVENT
            // parse below (same reason SF_CLOUD_EVENT is checked first: distinct
            // marker string, no risk of collision, but order still documents
            // precedence).
            if (TryParseSfPca9685Event(line, out var pca9685Address, out var servoComponentId, out var servoAngle))
            {
                var pcaTime = stopwatch.ElapsedMilliseconds;
                eventsEmitted++;
                await EmitAsync(eventStore, broadcaster, context.ProjectId, new SimulationEventResponse
                {
                    Type = "part-state",
                    Time = pcaTime,
                    Payload = new Dictionary<string, object?>
                    {
                        // partId/component/state/angle: EXACT same convention as
                        // ServoModel.cs's ToAngleEvent (Educational runner) so
                        // LabSandboxPage.tsx's single existing
                        // `component === 'servo' && payload.state === 'angle'`
                        // branch handles both runners without a new FE branch.
                        ["partId"] = servoComponentId,
                        ["component"] = "servo",
                        ["state"] = "angle",
                        ["angle"] = servoAngle,
                        ["address"] = pca9685Address,
                        ["driver"] = "pca9685"
                    }
                }, cancellationToken);
                continue;
            }

            if (TryParseSfCloudEvent(line, out var cloudComponentId, out var cloudTopic, out var cloudValue))
            {
                var cloudTime = stopwatch.ElapsedMilliseconds;
                await EmitAsync(eventStore, broadcaster, context.ProjectId, new SimulationEventResponse
                {
                    Type = "cloud-event",
                    Time = cloudTime,
                    Payload = new Dictionary<string, object?>
                    {
                        ["componentId"] = cloudComponentId,
                        ["topic"] = cloudTopic,
                        ["value"] = cloudValue,
                        ["timeMs"] = cloudTime
                    }
                }, cancellationToken);

                await EmitAsync(eventStore, broadcaster, context.ProjectId, new SimulationEventResponse
                {
                    Type = "serial",
                    Time = cloudTime,
                    Payload = new Dictionary<string, object?>
                    {
                        ["message"] = $"[cloud] {cloudTopic} = {cloudValue}",
                        ["newline"] = true
                    }
                }, cancellationToken);
                continue;
            }

            if (TryParseSfCloudLog(line, out var cloudLogComponentId, out var cloudLogMessage))
            {
                await EmitAsync(eventStore, broadcaster, context.ProjectId, new SimulationEventResponse
                {
                    Type = "serial",
                    Time = stopwatch.ElapsedMilliseconds,
                    Payload = new Dictionary<string, object?>
                    {
                        ["message"] = $"[cloud] {cloudLogMessage}",
                        ["newline"] = true
                    }
                }, cancellationToken);
                continue;
            }

            if (!TryParseSfEvent(line, out var pin, out var value))
            {
                // BUG THẬT vừa vá (2026-07-27): trước đây MỌI dòng không phải
                // SF_EVENT bị `continue` bỏ qua im lặng — nghĩa là boot log ROM
                // ESP32 thật ("ets Jul 29 2019...", "rst:0x1...") và
                // Serial.println() thật từ firmware người dùng ĐỀU bị đọc từ
                // serial.log rồi vứt đi, không bao giờ tới FE, dù
                // SerialMonitorPanel.tsx/applySimulationEvent đã hỗ trợ sẵn
                // type="serial" từ lâu (dùng cho EducationalSimulationRunner)
                // — QemuEsp32Runner (mode chạy firmware thật) chưa từng emit
                // loại event này. `-serial file:...` chỉ redirect đúng I/O của
                // UART0 giả lập (ROM bootloader + Serial người dùng nếu cùng
                // UART0) vào file này — không lẫn log nội bộ của QEMU/Docker
                // (đi stdout/stderr riêng, đã đọc ở nhánh STEM_QEMU_DEBUG bên
                // trên) nên an toàn stream nguyên văn, không cần lọc thêm.
                await EmitAsync(eventStore, broadcaster, context.ProjectId, new SimulationEventResponse
                {
                    Type = "serial",
                    Time = stopwatch.ElapsedMilliseconds,
                    Payload = new Dictionary<string, object?>
                    {
                        ["message"] = line,
                        ["newline"] = true
                    }
                }, cancellationToken);
                continue;
            }

            eventsEmitted++;
            var time = stopwatch.ElapsedMilliseconds;
            await EmitAsync(eventStore, broadcaster, context.ProjectId, new SimulationEventResponse
            {
                Type = "pin-state",
                Time = time,
                Payload = new Dictionary<string, object?>
                {
                    ["pin"] = pin,
                    ["value"] = value,
                    ["operation"] = "digitalWrite"
                }
            }, cancellationToken);

            foreach (var led in componentIndex.FindLeds(pin))
            {
                await EmitAsync(eventStore, broadcaster, context.ProjectId, led.ToDigitalEvent(time, value), cancellationToken);
            }

            foreach (var buzzer in componentIndex.FindBuzzers(pin))
            {
                await EmitAsync(eventStore, broadcaster, context.ProjectId, buzzer.ToDigitalEvent(time, value), cancellationToken);
            }

            foreach (var fan in componentIndex.FindFans(pin))
            {
                await EmitAsync(eventStore, broadcaster, context.ProjectId, fan.ToDigitalEvent(time, value), cancellationToken);
            }

            foreach (var droneMotor in componentIndex.FindDroneMotors(pin))
            {
                await EmitAsync(eventStore, broadcaster, context.ProjectId, droneMotor.ToDigitalEvent(time, value), cancellationToken);
            }

            // L298N: state động cơ phụ thuộc CẶP chân IN (không phải 1 chân
            // riêng lẻ như LED/Buzzer) — phải tính lại state TRƯỚC và SAU khi
            // cập nhật giá trị chân vừa nhận, chỉ emit khi thật sự đổi (tránh
            // spam event trùng mỗi lần digitalWrite dù kết quả logic không đổi,
            // vd ghi lại đúng giá trị cũ).
            if (componentIndex.TryFindL298nInPin(pin, out var l298n, out var inIndex))
            {
                var boolValue = value.Equals("HIGH", StringComparison.OrdinalIgnoreCase);
                var isMotorA = inIndex is 1 or 2;
                var prevState = isMotorA
                    ? L298nModel.ComputeState(l298n.In1Value, l298n.In2Value)
                    : L298nModel.ComputeState(l298n.In3Value, l298n.In4Value);

                switch (inIndex)
                {
                    case 1: l298n.In1Value = boolValue; break;
                    case 2: l298n.In2Value = boolValue; break;
                    case 3: l298n.In3Value = boolValue; break;
                    case 4: l298n.In4Value = boolValue; break;
                }

                var newState = isMotorA
                    ? L298nModel.ComputeState(l298n.In1Value, l298n.In2Value)
                    : L298nModel.ComputeState(l298n.In3Value, l298n.In4Value);

                if (newState != prevState)
                {
                    var motorLabel = isMotorA ? "A" : "B";
                    await EmitAsync(eventStore, broadcaster, context.ProjectId, l298n.ToMotorStateEvent(time, motorLabel, newState), cancellationToken);
                }
            }

            if (componentIndex.TryFindRgbLedPin(pin, out var rgbLed, out var channel))
            {
                await EmitAsync(eventStore, broadcaster, context.ProjectId, rgbLed.ToChannelEvent(time, channel, value), cancellationToken);
            }
        }

        return (newOffset, eventsEmitted);
    }

    private Process StartQemuProcess(string firmwarePath, string logDir, string containerName)
    {
        var dockerCliPath = _configuration["SimulationCompile:DockerCliPath"] ?? "docker";
        var dockerImage = _configuration["SimulationRunner:Qemu:DockerImage"] ?? "stem-qemu-runner-sandbox:latest";
        // 512m/1.0 cpu/64 pids đo thật: Blink LED liên tục peak ~157MB; test độc hại
        // (Serial spam vô hạn, malloc bomb, while(true) không yield) đều bị chặn/dọn
        // sạch trong giới hạn này — xem 3 test độc hại Bước 1.
        var memoryLimit = _configuration["SimulationRunner:Qemu:MemoryLimit"] ?? "512m";
        var cpuLimit = _configuration["SimulationRunner:Qemu:CpuLimit"] ?? "1.0";
        var pidsLimit = _configuration["SimulationRunner:Qemu:PidsLimit"] ?? "64";

        var process = new Process();
        process.StartInfo.FileName = dockerCliPath;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        var args = process.StartInfo.ArgumentList;
        args.Add("run");
        // KHÔNG "--rm" — xem comment dài ở finally block trong
        // ExecuteInBackgroundAsync (bug --rm + .NET Process trên Windows). Dọn
        // tường minh bằng TryDockerRemoveAsync ("docker rm -f") thay vì để
        // Docker tự xoá.
        args.Add("--name"); args.Add(containerName);
        args.Add("--network"); args.Add("none");
        args.Add("--memory"); args.Add(memoryLimit);
        args.Add("--memory-swap"); args.Add(memoryLimit);
        args.Add("--cpus"); args.Add(cpuLimit);
        args.Add("--pids-limit"); args.Add(pidsLimit);
        args.Add("--cap-drop"); args.Add("ALL");
        args.Add("--security-opt"); args.Add("no-new-privileges");
        args.Add("--read-only");
        args.Add("--user"); args.Add("10001:10001");
        args.Add("-v"); args.Add($"{firmwarePath}:/workspace/firmware.bin:ro");
        // Bind mount RIÊNG (không phải tmpfs) — QEMU ghi GPIO/serial log ra file ở
        // đây thay vì stdout, host đọc lại từ CHÍNH file này (ReadNewLogLinesAsync)
        // — --read-only ở container không ảnh hưởng bind mount rw riêng lẻ này.
        args.Add("-v"); args.Add($"{logDir}:/workspace/log:rw");
        args.Add("--tmpfs"); args.Add("/tmp:rw,size=32m,mode=1777");
        args.Add(dockerImage);
        // KHÔNG dùng "-nographic" (multiplex serial+monitor chung 1 stdio, cần tty
        // raw mode) — xác nhận thật: dưới pipe (đúng những gì .NET
        // RedirectStandardOutput luôn tạo, không bao giờ là tty), QEMU tự thoát sạch
        // sau vài giây, có lúc trước khi in được sự kiện đầu tiên. Tách riêng
        // display/monitor/serial: serial ra FILE (đọc qua bind mount, không qua
        // stdio) để không lệ thuộc tty/pipe gì cả — xem VIRTUAL_LAB_PLAN.md 8.13.
        args.Add("-display"); args.Add("none");
        args.Add("-monitor"); args.Add("none");
        args.Add("-serial"); args.Add("file:/workspace/log/serial.log");
        args.Add("-no-reboot");
        args.Add("-M"); args.Add("esp32");
        // readonly=on bắt buộc: QEMU mặc định mở -drive if=mtd ở chế độ ghi dù input
        // chỉ cần đọc — thiếu cờ này bind mount :ro sẽ báo lỗi "Read-only file system"
        // (đã xác nhận thật khi verify, không phải suy đoán).
        args.Add("-drive"); args.Add("file=/workspace/firmware.bin,if=mtd,format=raw,readonly=on");

        return process;
    }

    // "rm -f" vừa kill (nếu đang chạy) vừa xoá container trong 1 lệnh — cần
    // thiết vì StartQemuProcess không còn "--rm" nữa (xem comment ở
    // ExecuteInBackgroundAsync).
    private static async Task TryDockerRemoveAsync(string containerName)
    {
        try
        {
            using var removeProcess = new Process();
            removeProcess.StartInfo.FileName = "docker";
            removeProcess.StartInfo.ArgumentList.Add("rm");
            removeProcess.StartInfo.ArgumentList.Add("-f");
            removeProcess.StartInfo.ArgumentList.Add(containerName);
            removeProcess.StartInfo.RedirectStandardOutput = true;
            removeProcess.StartInfo.RedirectStandardError = true;
            removeProcess.StartInfo.UseShellExecute = false;
            removeProcess.StartInfo.CreateNoWindow = true;
            removeProcess.Start();
            await removeProcess.StandardOutput.ReadToEndAsync();
            await removeProcess.StandardError.ReadToEndAsync();
            await removeProcess.WaitForExitAsync();
        }
        catch
        {
            // Best-effort — container có thể đã bị xoá trước đó, không phải lỗi.
        }
    }

    private static void TryKillProcess(Process? process)
    {
        try
        {
            if (process != null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
        }
    }

    private string ResolveWorkingRoot()
    {
        var workingRoot = _configuration["SimulationRunner:Qemu:WorkingDirectory"];
        return string.IsNullOrWhiteSpace(workingRoot)
            ? Path.Combine(Path.GetTempPath(), "stem-qemu-run")
            : workingRoot;
    }

    // DiagramJson (đã qua VirtualLabDiagramService.Analyze) luôn có field "board" cấp
    // cao nhất — cùng cơ chế đã dùng để vá bug "Connection references invalid part:
    // arduino" (xem STREAMING_SIMULATION_PLAN.md/backlog VIRTUAL_LAB_PLAN.md).
    private static string ExtractBoard(string diagramJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(diagramJson);
            if (doc.RootElement.TryGetProperty("board", out var boardEl) && boardEl.ValueKind == JsonValueKind.String)
            {
                var value = boardEl.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        catch (JsonException)
        {
        }

        return "esp32";
    }

    private static bool IsAvrBoard(string board)
    {
        return board.Contains("uno", StringComparison.OrdinalIgnoreCase) ||
               board.Contains("avr", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseSfEvent(string line, out string pin, out string value)
    {
        pin = string.Empty;
        value = string.Empty;

        var markerIndex = line.IndexOf("SF_EVENT ", StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        try
        {
            var jsonPart = line[(markerIndex + "SF_EVENT ".Length)..].Trim();
            using var doc = JsonDocument.Parse(jsonPart);
            var root = doc.RootElement;
            if (!root.TryGetProperty("pin", out var pinEl) || !root.TryGetProperty("value", out var valueEl))
            {
                return false;
            }

            // NormalizeGpio dùng chung quy ước với VirtualLabDiagramService: bỏ tiền
            // tố "GPIO", chỉ giữ số — khớp đúng key trong PinToGpio đã build từ
            // VirtualLabDiagramService.BuildRuntimeSnapshot.
            var rawPin = pinEl.GetString() ?? string.Empty;
            pin = rawPin.StartsWith("GPIO", StringComparison.OrdinalIgnoreCase) ? rawPin[4..] : rawPin;
            value = valueEl.GetString() ?? string.Empty;
            return pin.Length > 0 && value.Length > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // VIRTUAL LAB RUNTIME CAPABILITY EXPANSION (STEP 3) — parses
    // StemFlowPCA9685's SF_PCA9685_EVENT lines. angle is always an int (0-180,
    // already clamped by StemFlowPCA9685::setServoAngle before printing).
    private static bool TryParseSfPca9685Event(string line, out string address, out string servoId, out int angle)
    {
        address = string.Empty;
        servoId = string.Empty;
        angle = 0;

        var markerIndex = line.IndexOf("SF_PCA9685_EVENT ", StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        try
        {
            var jsonPart = line[(markerIndex + "SF_PCA9685_EVENT ".Length)..].Trim();
            using var doc = JsonDocument.Parse(jsonPart);
            var root = doc.RootElement;
            if (!root.TryGetProperty("address", out var addressEl) ||
                !root.TryGetProperty("servoId", out var servoIdEl) ||
                !root.TryGetProperty("angle", out var angleEl))
            {
                return false;
            }

            address = addressEl.GetString() ?? string.Empty;
            servoId = servoIdEl.GetString() ?? string.Empty;
            angle = angleEl.GetInt32();
            return address.Length > 0 && servoId.Length > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // WiFi/Cloud Phase 1 — value có thể là số (publish float/int) hoặc chuỗi
    // (dự phòng, hiện StemFlowCloud chỉ publish số) -> giữ kiểu gốc từ JSON
    // (double/string/bool) thay vì ép hết về string, để FE nhận đúng kiểu qua
    // SignalR/JSON serialize lại. JSON lỗi -> false + cảnh báo ra stderr BE,
    // KHÔNG throw/crash (dòng sẽ rơi xuống nhánh raw-serial-passthrough phía
    // dưới, học sinh vẫn thấy dòng gốc trong Serial Monitor để debug).
    private static bool TryParseSfCloudEvent(string line, out string componentId, out string topic, out object? value)
    {
        componentId = string.Empty;
        topic = string.Empty;
        value = null;

        var markerIndex = line.IndexOf("SF_CLOUD_EVENT ", StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        try
        {
            var jsonPart = line[(markerIndex + "SF_CLOUD_EVENT ".Length)..].Trim();
            using var doc = JsonDocument.Parse(jsonPart);
            var root = doc.RootElement;
            if (!root.TryGetProperty("componentId", out var idEl) || !root.TryGetProperty("topic", out var topicEl))
            {
                return false;
            }

            componentId = idEl.GetString() ?? string.Empty;
            topic = topicEl.GetString() ?? string.Empty;
            if (root.TryGetProperty("value", out var valueEl))
            {
                value = valueEl.ValueKind switch
                {
                    JsonValueKind.Number => valueEl.TryGetDouble(out var d) ? d : null,
                    JsonValueKind.String => valueEl.GetString(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => null
                };
            }

            return componentId.Length > 0 && topic.Length > 0;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"[cloud] SF_CLOUD_EVENT JSON không hợp lệ, bỏ qua dòng này: {ex.Message}");
            return false;
        }
    }

    private static bool TryParseSfCloudLog(string line, out string componentId, out string message)
    {
        componentId = string.Empty;
        message = string.Empty;

        var markerIndex = line.IndexOf("SF_CLOUD_LOG ", StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        try
        {
            var jsonPart = line[(markerIndex + "SF_CLOUD_LOG ".Length)..].Trim();
            using var doc = JsonDocument.Parse(jsonPart);
            var root = doc.RootElement;
            if (!root.TryGetProperty("componentId", out var idEl) || !root.TryGetProperty("message", out var msgEl))
            {
                return false;
            }

            componentId = idEl.GetString() ?? string.Empty;
            message = msgEl.GetString() ?? string.Empty;
            return componentId.Length > 0;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"[cloud] SF_CLOUD_LOG JSON không hợp lệ, bỏ qua dòng này: {ex.Message}");
            return false;
        }
    }

    // Index GPIO -> component riêng cho runner này (không tái dùng nội bộ private của
    // EducationalEventGenerator — cố tình không đụng file interpreter cũ, giữ 2 runner
    // hoàn toàn độc lập).
    private sealed class ComponentIndex
    {
        private readonly Dictionary<string, List<LedModel>> _ledsByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<BuzzerModel>> _buzzersByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<FanModel>> _fansByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<DroneMotorModel>> _droneMotorsByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (L298nModel Model, int InIndex)> _l298nByPin = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (RgbLedModel Model, string Channel)> _rgbLedByPin = new(StringComparer.OrdinalIgnoreCase);

        public ComponentIndex(VirtualLabRuntimeDiagramSnapshot snapshot)
        {
            foreach (var component in snapshot.Components)
            {
                if (component.Type.Equals("wokwi-led", StringComparison.OrdinalIgnoreCase) &&
                    TryFindPin(component, new[] { "A" }, out var ledPin))
                {
                    Add(_ledsByPin, ledPin, new LedModel(component.Id, ledPin));
                }
                else if (component.Type.Equals("wokwi-buzzer", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "1", "2" }, out var buzzerPin))
                {
                    Add(_buzzersByPin, buzzerPin, new BuzzerModel(component.Id, buzzerPin));
                }
                else if (component.Type.Equals("wokwi-fan", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "IN" }, out var fanPin))
                {
                    Add(_fansByPin, fanPin, new FanModel(component.Id, fanPin));
                }
                else if (component.Type.Equals("wokwi-drone-motor", StringComparison.OrdinalIgnoreCase) &&
                         TryFindPin(component, new[] { "IN" }, out var droneMotorPin))
                {
                    Add(_droneMotorsByPin, droneMotorPin, new DroneMotorModel(component.Id, droneMotorPin));
                }
                else if (component.Type.Equals("wokwi-l298n", StringComparison.OrdinalIgnoreCase))
                {
                    component.PinToGpio.TryGetValue("IN1", out var in1);
                    component.PinToGpio.TryGetValue("IN2", out var in2);
                    component.PinToGpio.TryGetValue("IN3", out var in3);
                    component.PinToGpio.TryGetValue("IN4", out var in4);
                    var model = new L298nModel(component.Id, in1, in2, in3, in4);
                    if (!string.IsNullOrEmpty(in1)) _l298nByPin[in1] = (model, 1);
                    if (!string.IsNullOrEmpty(in2)) _l298nByPin[in2] = (model, 2);
                    if (!string.IsNullOrEmpty(in3)) _l298nByPin[in3] = (model, 3);
                    if (!string.IsNullOrEmpty(in4)) _l298nByPin[in4] = (model, 4);
                }
                else if (component.Type.Equals("wokwi-rgb-led", StringComparison.OrdinalIgnoreCase))
                {
                    component.PinToGpio.TryGetValue("R", out var r);
                    component.PinToGpio.TryGetValue("G", out var g);
                    component.PinToGpio.TryGetValue("B", out var b);
                    var model = new RgbLedModel(component.Id, r, g, b);
                    if (!string.IsNullOrEmpty(r)) _rgbLedByPin[r] = (model, "R");
                    if (!string.IsNullOrEmpty(g)) _rgbLedByPin[g] = (model, "G");
                    if (!string.IsNullOrEmpty(b)) _rgbLedByPin[b] = (model, "B");
                }
            }
        }

        public IReadOnlyCollection<LedModel> FindLeds(string pin) => Find(_ledsByPin, pin);
        public IReadOnlyCollection<BuzzerModel> FindBuzzers(string pin) => Find(_buzzersByPin, pin);
        public IReadOnlyCollection<FanModel> FindFans(string pin) => Find(_fansByPin, pin);
        public IReadOnlyCollection<DroneMotorModel> FindDroneMotors(string pin) => Find(_droneMotorsByPin, pin);

        public bool TryFindL298nInPin(string pin, out L298nModel model, out int inIndex)
        {
            if (_l298nByPin.TryGetValue(pin, out var entry))
            {
                model = entry.Model;
                inIndex = entry.InIndex;
                return true;
            }

            model = null!;
            inIndex = 0;
            return false;
        }

        public bool TryFindRgbLedPin(string pin, out RgbLedModel model, out string channel)
        {
            if (_rgbLedByPin.TryGetValue(pin, out var entry))
            {
                model = entry.Model;
                channel = entry.Channel;
                return true;
            }

            model = null!;
            channel = string.Empty;
            return false;
        }

        private static bool TryFindPin(
            VirtualLabRuntimeComponent component,
            IReadOnlyCollection<string> candidatePins,
            out string gpioPin)
        {
            foreach (var pin in candidatePins)
            {
                if (component.PinToGpio.TryGetValue(pin, out gpioPin!))
                {
                    return true;
                }
            }

            gpioPin = component.PinToGpio.Values.FirstOrDefault() ?? string.Empty;
            return gpioPin.Length > 0;
        }

        private static IReadOnlyCollection<T> Find<T>(IReadOnlyDictionary<string, List<T>> index, string pin)
        {
            return index.TryGetValue(pin, out var matches) ? matches : Array.Empty<T>();
        }

        private static void Add<T>(Dictionary<string, List<T>> index, string pin, T model)
        {
            if (!index.TryGetValue(pin, out var models))
            {
                models = new List<T>();
                index[pin] = models;
            }

            models.Add(model);
        }
    }
}
