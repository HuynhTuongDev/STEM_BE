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
    private const string InstrumentationVersion = "ets_printf_v4_pca9685_esp32_2_0_17";
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

        // ---- StemFlow I2C bus minimum viable runtime (auto-injected, VIRTUAL LAB
        // RUNTIME CAPABILITY EXPANSION task, 2026-08-26) ----
        // KHÔNG dùng thư viện Wire.h thật (chưa từng verify có sẵn trong sandbox
        // compile này — cùng rủi ro đã tránh với Servo.h ở milestone trước) và
        // KHÔNG mô phỏng timing I2C thật ở mức xung điện (QEMU không expose điều
        // đó cho tầng firmware theo cách quan sát được — cùng lý do StemFlowDHT
        // tồn tại thay vì DHT.h thật). Đây là MỘT BUS MODEL THẬT chạy THẬT bên
        // trong firmware đã compile/boot qua QEMU thật — device address, đăng ký
        // thiết bị, phát hiện trùng địa chỉ, phát hiện địa chỉ không tồn tại, và
        // giao dịch đọc/ghi đều là logic C++ THẬT thực thi trên CPU thật của QEMU,
        // không phải giá trị hardcode từ bên ngoài.
        class StemFlowI2C {
        public:
          static bool registerDevice(uint8_t address) {
            for (int i = 0; i < _deviceCount; i++) {
              if (_addresses[i] == address) {
                ets_printf("I2C: LOI dia chi trung lap 0x%02X\n", address);
                return false;
              }
            }
            if (_deviceCount >= MAX_DEVICES) {
              ets_printf("I2C: LOI qua nhieu thiet bi tren bus (toi da %d)\n", MAX_DEVICES);
              return false;
            }
            _addresses[_deviceCount] = address;
            _deviceCount++;
            ets_printf("I2C: da dang ky thiet bi 0x%02X\n", address);
            return true;
          }

          static bool isRegistered(uint8_t address) {
            for (int i = 0; i < _deviceCount; i++) {
              if (_addresses[i] == address) return true;
            }
            return false;
          }

          static bool writeRegister(uint8_t address, uint8_t reg, uint8_t value) {
            if (!isRegistered(address)) {
              ets_printf("I2C: LOI ghi dia chi khong ton tai 0x%02X\n", address);
              return false;
            }
            _lastReg[address] = reg;
            _lastValue[address] = value;
            ets_printf("I2C: ghi 0x%02X reg=%d value=%d\n", address, reg, value);
            return true;
          }

          static int readRegister(uint8_t address, uint8_t reg) {
            if (!isRegistered(address)) {
              ets_printf("I2C: LOI doc dia chi khong ton tai 0x%02X\n", address);
              return -1;
            }
            ets_printf("I2C: doc 0x%02X reg=%d value=%d\n", address, reg, _lastValue[address]);
            return _lastValue[address];
          }

        private:
          static const int MAX_DEVICES = 8;
          static uint8_t _addresses[MAX_DEVICES];
          static int _deviceCount;
          static uint8_t _lastReg[128];
          static uint8_t _lastValue[128];
        };
        uint8_t StemFlowI2C::_addresses[StemFlowI2C::MAX_DEVICES];
        int StemFlowI2C::_deviceCount = 0;
        uint8_t StemFlowI2C::_lastReg[128];
        uint8_t StemFlowI2C::_lastValue[128];
        // ---- end StemFlow I2C bus minimum viable runtime ----

        // ---- StemFlow PCA9685 runtime model (STEP 3, 2026-08-26) ----
        // KHÔNG dùng thư viện Adafruit_PWMServoDriver thật (chưa verify có sẵn
        // trong sandbox — cùng lý do tránh Wire.h/Servo.h ở trên). Đi qua
        // StemFlowI2C THẬT ở trên (registerDevice/writeRegister thật, không bỏ
        // qua bus layer) — góc servo mỗi kênh là state THẬT do CHÍNH chương
        // trình gọi setServoAngle() quyết định, không hardcode. servoId khớp
        // đúng id linh kiện wokwi-servo trong diagram — CÙNG QUY ƯỚC với
        // StemFlowDHT("dht1") (so khớp theo ID linh kiện, không theo pin vật lý)
        // vì góc servo qua PCA9685 không có cách nào quan sát qua 1 chân GPIO
        // đơn lẻ như digitalWrite (PWM_QEMU_GAP) — SF_PCA9685_EVENT là kênh
        // quan sát duy nhất, tương tự SF_EVENT cho digitalWrite.
        class StemFlowPCA9685 {
        public:
          StemFlowPCA9685(uint8_t address) : _address(address) {
            StemFlowI2C::registerDevice(_address);
          }

          void setServoAngle(const char* servoId, int angle) {
            int clamped = angle;
            if (clamped < 0) clamped = 0;
            if (clamped > 180) clamped = 180;
            StemFlowI2C::writeRegister(_address, 0, (uint8_t)clamped);
            ets_printf("SF_PCA9685_EVENT {\"address\":\"0x%02X\",\"servoId\":\"%s\",\"angle\":%d}\n", _address, servoId, clamped);
          }

        private:
          uint8_t _address;
        };
        // ---- end StemFlow PCA9685 runtime model ----

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
        CancellationToken cancellationToken,
        string sensorHeader = "")
    {
        var cacheDir = ResolveCacheDir(sourceCode, board, framework, sensorHeader);
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
        CancellationToken cancellationToken,
        string sensorHeader = "",
        IReadOnlyDictionary<string, string>? extraFiles = null)
    {
        var cacheDir = ResolveCacheDir(sourceCode, board, framework, sensorHeader);
        var cacheKey = Path.GetFileName(cacheDir);

        // Single-flight qua ICompileCoordinator — nếu 1 caller khác (vd nền
        // precompile lúc gõ code) đang compile ĐÚNG cacheKey này rồi, caller
        // này chỉ đợi lại kết quả đó, không khởi compile thật lần 2. Việc
        // compile+ghi cache LUÔN chạy trọn vẹn với CancellationToken.None dù
        // caller gốc có bỏ cuộc hay không — kết quả vẫn có ích cho cache.
        return _coordinator.GetOrCompileAsync(
            cacheKey,
            () => CompileAndWriteCacheCoreAsync(sourceCode, board, framework, buildCacheScopeId, cacheDir, cacheKey, sensorHeader, extraFiles),
            cancellationToken);
    }

    private async Task<CompileSimulationResponse> CompileAndWriteCacheCoreAsync(
        string sourceCode,
        string board,
        string framework,
        Guid? buildCacheScopeId,
        string cacheDir,
        string cacheKey,
        string sensorHeader,
        IReadOnlyDictionary<string, string>? extraFiles)
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
        var existing = await TryGetCachedFirmwareAsync(sourceCode, board, framework, CancellationToken.None, sensorHeader);
        if (existing != null)
        {
            return existing;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // sensorHeader (Sensor Input Bridge, xem SensorRuntimeHeaderGenerator.cs)
        // đứng SAU GpioInstrumentationPreamble, TRƯỚC sourceCode học sinh — rỗng
        // "" theo mặc định (nối rỗng vào chuỗi không đổi gì, instrumentedSource
        // giữ nguyên byte-for-byte như trước khi có tính năng này).
        var instrumentedSource = GpioInstrumentationPreamble + sensorHeader + sourceCode;
        var compileResult = await _compileService.CompileAsync(new CompileSimulationRequest
        {
            SourceCode = instrumentedSource,
            Board = board,
            Framework = framework,
            ProjectId = buildCacheScopeId?.ToString("N"),
            ExtraFiles = extraFiles
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

    private string ResolveCacheDir(string sourceCode, string board, string framework, string sensorHeader = "")
    {
        var fqbn = SimulationCompileService.NormalizeBoard(board);
        var keyInput = string.Join(
            '',
            sourceCode,
            board,
            framework,
            fqbn,
            InstrumentationVersion,
            RunnerMode,
            sensorHeader);
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
