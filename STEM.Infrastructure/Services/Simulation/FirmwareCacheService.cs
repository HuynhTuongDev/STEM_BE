using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;

namespace STEM.Infrastructure.Services.Simulation;

public sealed class FirmwareCacheService : IFirmwareCacheService
{
    // Bump khi đổi nội dung GpioInstrumentationPreamble HOẶC core/toolchain
    // trong stem-arduino-cli-sandbox image — firmware cache cũ (compile với
    // bản trước đó) phải bị coi là miss, không được tái sử dụng nhầm. Bump lần
    // này (v1 -> v2_esp32_2_0_17): pin lại esp32:esp32 core từ 3.3.10 xuống
    // 2.0.17 trong Dockerfile — 3.3.10 crash không tất định trong QEMU (Guru
    // Meditation "Cache error", tái hiện cả với sketch không có instrumentation
    // gì), 2.0.17 verify sạch qua 3 lần chạy độc lập. Firmware cache cũ (build
    // với 3.3.10, cùng nội dung source) phải bị bỏ, không được coi là hit —
    // nếu không học sinh sẽ nhận lại đúng firmware hay crash đó. Xem
    // VIRTUAL_LAB_PLAN.md mục 8.12/8.13.
    private const string InstrumentationVersion = "ets_printf_v2_esp32_2_0_17";
    private const string RunnerMode = "qemu";
    private const int CompileUserId = 0; // ISimulationCompileService.CompileAsync nhận currentUserId nhưng không dùng tới — xem QemuEsp32Runner.

    // #define phải đặt SAU khi hàm wrapper được định nghĩa xong — nếu đặt trước, lời
    // gọi digitalWrite(...) THẬT bên trong chính wrapper cũng bị macro thay thế thành
    // gọi lại chính nó, đệ quy vô hạn (đã xác nhận: bug này có thật khi thử ngược thứ
    // tự — sửa bằng cách định nghĩa hàm trước). ets_printf (không phải
    // Serial.println) — đã verify thật: Serial.println gây crash không tất định
    // "Guru Meditation Error: Cache error" dưới QEMU, ets_printf (hàm UART cấp thấp
    // trong ROM, bỏ qua driver C++ HardwareSerial) thì sạch 100% qua nhiều lần test.
    private const string GpioInstrumentationPreamble = """
        // ---- StemFlow QEMU GPIO instrumentation (auto-injected, không phải code học sinh) ----
        extern "C" int ets_printf(const char *fmt, ...);
        IRAM_ATTR void __sf_digitalWrite(int pin, int value) {
          digitalWrite(pin, value);
          ets_printf("SF_EVENT {\"pin\":\"GPIO%d\",\"value\":\"%s\"}\n", pin, value ? "HIGH" : "LOW");
        }
        #define digitalWrite(pin, val) __sf_digitalWrite(pin, val)
        // ---- end StemFlow QEMU GPIO instrumentation ----

        """;

    private readonly ISimulationCompileService _compileService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FirmwareCacheService> _logger;
    private readonly ICompileCoordinator _coordinator;

    public FirmwareCacheService(
        ISimulationCompileService compileService,
        IConfiguration configuration,
        ILogger<FirmwareCacheService> logger,
        ICompileCoordinator coordinator)
    {
        _compileService = compileService;
        _configuration = configuration;
        _logger = logger;
        _coordinator = coordinator;
    }

    public async Task<CompileSimulationResponse?> TryGetCachedFirmwareAsync(
        string sourceCode,
        string board,
        string framework,
        CancellationToken cancellationToken)
    {
        var cacheDir = ResolveCacheDir(sourceCode, board, framework);
        var cacheKey = Path.GetFileName(cacheDir);
        var firmwarePath = Path.Combine(cacheDir, "firmware.bin");
        var metaPath = Path.Combine(cacheDir, "meta.json");

        if (!File.Exists(firmwarePath) || !File.Exists(metaPath))
        {
            _logger.LogInformation("Firmware cache MISS {CacheKey}: chưa từng compile (không thấy firmware.bin/meta.json).", cacheKey);
            return null;
        }

        try
        {
            var meta = JsonSerializer.Deserialize<CacheMeta>(await File.ReadAllTextAsync(metaPath, cancellationToken));
            if (meta == null)
            {
                _logger.LogWarning("Firmware cache MISS {CacheKey}: meta.json rỗng/không đọc được.", cacheKey);
                return null;
            }

            var firmwareBytes = await File.ReadAllBytesAsync(firmwarePath, cancellationToken);
            _logger.LogInformation("Firmware cache HIT {CacheKey}: dùng lại firmware {SizeBytes} bytes, bỏ qua compile.", cacheKey, firmwareBytes.Length);
            return new CompileSimulationResponse
            {
                Success = true,
                FirmwareBase64 = Convert.ToBase64String(firmwareBytes),
                FirmwareFileName = meta.FirmwareFileName,
                FirmwareFormat = meta.FirmwareFormat,
                Errors = Array.Empty<CompileSimulationError>()
            };
        }
        catch (Exception ex)
        {
            // Cache hỏng (ghi dở, file thiếu, JSON lỗi...) — coi như miss, để
            // caller compile lại bình thường thay vì trả lỗi cứng.
            _logger.LogWarning(ex, "Firmware cache MISS {CacheKey}: đọc cache lỗi, coi như chưa có.", cacheKey);
            return null;
        }
    }

    public Task<CompileSimulationResponse> CompileAndCacheAsync(
        string sourceCode,
        string board,
        string framework,
        Guid? buildCacheScopeId,
        CancellationToken cancellationToken)
    {
        var cacheDir = ResolveCacheDir(sourceCode, board, framework);
        var cacheKey = Path.GetFileName(cacheDir);

        // Single-flight qua ICompileCoordinator — nếu 1 caller khác (vd nền
        // precompile lúc gõ code) đang compile ĐÚNG cacheKey này rồi, caller
        // này chỉ đợi lại kết quả đó, không khởi compile thật lần 2. Việc
        // compile+ghi cache LUÔN chạy trọn vẹn với CancellationToken.None dù
        // caller gốc có bỏ cuộc hay không — kết quả vẫn có ích cho cache.
        return _coordinator.GetOrCompileAsync(
            cacheKey,
            () => CompileAndWriteCacheCoreAsync(sourceCode, board, framework, buildCacheScopeId, cacheDir, cacheKey),
            cancellationToken);
    }

    private async Task<CompileSimulationResponse> CompileAndWriteCacheCoreAsync(
        string sourceCode,
        string board,
        string framework,
        Guid? buildCacheScopeId,
        string cacheDir,
        string cacheKey)
    {
        // Kiểm tra lại cache NGAY TRONG closure được ICompileCoordinator bảo vệ
        // (không phải trước khi gọi coordinator) — bắt đúng trường hợp: 1 lần
        // gọi CompileAndCacheAsync trước đó (vd precompile lúc giáo viên lưu
        // Lab nhiều lần liên tiếp không đổi starterCode) đã compile+cache xong
        // TỪ TRƯỚC, nhưng caller hiện tại không tự kiểm tra cache trước khi
        // gọi (khác QemuEsp32Runner, nơi ĐÃ tự check trước để quyết định có
        // phát StudentCompileStarted hay không). Không có bước này,
        // CompileAndCacheAsync sẽ compile lại lãng phí ~40-90s mỗi lần gọi dù
        // nội dung không đổi.
        var existing = await TryGetCachedFirmwareAsync(sourceCode, board, framework, CancellationToken.None);
        if (existing != null)
        {
            return existing;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var instrumentedSource = GpioInstrumentationPreamble + sourceCode;
        var compileResult = await _compileService.CompileAsync(new CompileSimulationRequest
        {
            SourceCode = instrumentedSource,
            Board = board,
            Framework = framework,
            ProjectId = buildCacheScopeId?.ToString("N")
        }, CompileUserId, CancellationToken.None);

        if (!compileResult.Success || string.IsNullOrEmpty(compileResult.FirmwareBase64))
        {
            _logger.LogWarning(
                "Compile FAILED {CacheKey} sau {ElapsedMs}ms: {Errors}",
                cacheKey,
                stopwatch.ElapsedMilliseconds,
                string.Join("; ", compileResult.Errors.Select(e => e.Message)));
            return compileResult;
        }

        _logger.LogInformation("Compile OK {CacheKey} sau {ElapsedMs}ms, đang lưu firmware cache.", cacheKey, stopwatch.ElapsedMilliseconds);

        try
        {
            Directory.CreateDirectory(cacheDir);
            await File.WriteAllBytesAsync(
                Path.Combine(cacheDir, "firmware.bin"),
                Convert.FromBase64String(compileResult.FirmwareBase64));
            await File.WriteAllTextAsync(
                Path.Combine(cacheDir, "meta.json"),
                JsonSerializer.Serialize(new CacheMeta(compileResult.FirmwareFileName, compileResult.FirmwareFormat)));

            TryCleanupStaleFirmwareCaches(ResolveCacheRoot(), exceptDir: cacheDir);
        }
        catch (Exception ex)
        {
            // Ghi cache thất bại không được phép làm hỏng kết quả compile thật
            // vừa thành công — học sinh vẫn nhận đúng firmware, chỉ là lần sau
            // sẽ phải compile lại (mất cache, không mất tính đúng đắn).
            _logger.LogWarning(ex, "Ghi firmware cache {CacheKey} thất bại (không ảnh hưởng kết quả compile vừa xong).", cacheKey);
        }

        return compileResult;
    }

    private string ResolveCacheRoot()
    {
        var workingRoot = _configuration["SimulationCompile:WorkingDirectory"];
        if (string.IsNullOrWhiteSpace(workingRoot))
        {
            workingRoot = Path.Combine(Path.GetTempPath(), "stem-simulation-compile");
        }

        return Path.Combine(workingRoot, "firmware-cache");
    }

    private string ResolveCacheDir(string sourceCode, string board, string framework)
    {
        var fqbn = SimulationCompileService.NormalizeBoard(board);
        var keyInput = string.Join(
            '',
            sourceCode,
            board,
            framework,
            fqbn,
            InstrumentationVersion,
            RunnerMode);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(keyInput))).ToLowerInvariant();
        return Path.Combine(ResolveCacheRoot(), hash);
    }

    // Cùng kiểu dọn cơ hội như build-cache theo project (SimulationCompileService)
    // — retention 7 ngày, chạy ngay sau mỗi lần ghi cache mới, không cần cron
    // riêng.
    private static void TryCleanupStaleFirmwareCaches(string cacheRoot, string exceptDir)
    {
        try
        {
            if (!Directory.Exists(cacheRoot))
            {
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(-7);
            foreach (var dir in Directory.EnumerateDirectories(cacheRoot))
            {
                if (string.Equals(dir, exceptDir, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Directory.GetLastWriteTimeUtc(dir) < cutoff)
                {
                    try
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
        }
    }

    private sealed record CacheMeta(string? FirmwareFileName, string? FirmwareFormat);
}
