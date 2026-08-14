using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Simulations;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class SimulationCompileService : ISimulationCompileService
{
    private const int MaxCodeLength = 200_000;
    // FlashMode=dio (thay vì mặc định qio của board esp32:esp32:esp32) là bắt buộc
    // để firmware boot được trong QEMU (đã verify thật ở Bước 0 — QIO khiến QEMU
    // crash "Failed to set QIE bit", QEMU không giả lập đúng chuỗi trạng thái QE bit
    // của flash thật, không có cờ QEMU nào thay thế được). Áp dụng làm mặc định DUY
    // NHẤT cho MỌI lần compile ESP32 (không chỉ nhánh QEMU) — đã xác nhận: khác biệt
    // QIO/DIO chỉ ảnh hưởng tốc độ đọc flash trên PHẦN CỨNG THẬT, mà không luồng nào
    // trong hệ thống này (grading, anti-cheat Submit) từng nạp firmware vào phần
    // cứng — dùng 1 cấu hình duy nhất, tránh phải compile 2 lần với 2 FQBN khác nhau.
    // Không áp dụng cho arduino:avr:uno — board AVR không có khái niệm FlashMode
    // (xác nhận qua `arduino-cli board details`, option này chỉ tồn tại ở board esp32).
    private static readonly IReadOnlyDictionary<string, string> SupportedBoards =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["arduino:avr:uno"] = "arduino:avr:uno",
            ["arduino_uno"] = "arduino:avr:uno",
            ["uno"] = "arduino:avr:uno",
            ["esp32_devkit_v1"] = "esp32:esp32:esp32:FlashMode=dio",
            ["esp32"] = "esp32:esp32:esp32:FlashMode=dio",
            ["esp32dev"] = "esp32:esp32:esp32:FlashMode=dio",
            ["esp32:esp32:esp32"] = "esp32:esp32:esp32:FlashMode=dio",
            ["esp32:esp32:esp32dev"] = "esp32:esp32:esp32dev:FlashMode=dio"
        };
    private const int DefaultMaxConcurrentCompiles = 4;

    // BUG THẬT (2026-07-26): build-cache theo project (VIỆC B, keyed CHỈ theo
    // ProjectId) và golden-build-cache trên đĩa (EnsureGoldenBuildCacheAsync)
    // KHÔNG hề gắn với version core/toolchain trong image — khi pin
    // esp32:esp32 từ 3.3.10 xuống 2.0.17 (xem Dockerfile), 2 cache này vẫn
    // còn nguyên object file .o CŨ (biên dịch với 3.3.10). arduino-cli tái sử
    // dụng .o core CŨ + chỉ compile lại sketch.ino.cpp MỚI (2.0.17) → linkage
    // lẫn ABI 2 core khác nhau trong CÙNG 1 file .elf — verify thật: firmware
    // build kiểu này boot xong nhưng QEMU báo "esp32_i2c: Invalid command",
    // "esp_uart: read UART FIFO while it is empty", treo vĩnh viễn ở CPU
    // 99% không bao giờ chạy tới digitalWrite() (khác hẳn Guru Meditation
    // crash của core 3.3.10 gốc — đây là bug MỚI do chính lần fix core gây
    // ra, không dọn build-cache cũ). Bump chuỗi này mỗi khi core/toolchain
    // trong image đổi — buộc build-cache cũ (đánh dấu version khác/không có
    // marker) phải bị XOÁ SẠCH thay vì tái dùng object file không tương
    // thích. Xem VIRTUAL_LAB_PLAN.md 8.16.
    private const string ToolchainVersion = "esp32_2_0_17";
    private const string ToolchainVersionMarkerFileName = ".stemflow-toolchain-version";
    private static readonly Regex LineErrorRegex = new(@"(?:^|\s)(?<line>\d+):\d+:\s+(?<message>.+)$", RegexOptions.Compiled);
    private static readonly ConcurrentDictionary<string, CompileJobResponse> Jobs = new();
    private static SemaphoreSlim? _compileGate;
    private static readonly object CompileGateLock = new();

    // "Golden" build-path — warm-up 1 LẦN DUY NHẤT cho toàn tiến trình (lazy,
    // lần đầu có project/scope mới cần tới), rồi COPY (không share ghi trực
    // tiếp — tránh đúng bug lẫn dữ liệu đã phát hiện khi 2 compile ghi chung 1
    // build-path) sang build-cache riêng của TỪNG project/scope mới. Đo thật:
    // giảm core-compile phase từ ~31.37s xuống ~10s ngay cả với sketch hoàn
    // toàn khác nội dung — vì phần lớn file .o của core framework ESP32 không
    // phụ thuộc sketch cụ thể. Sketch "vàng" cố tình bao phủ nhiều API phổ
    // biến (Serial, digitalRead, analogRead, GPIO instrumentation thật) để
    // core-cache hữu ích cho nhiều loại sketch học sinh viết nhất có thể.
    private const string GoldenSketchSource = """
        extern "C" int ets_printf(const char *fmt, ...);
        IRAM_ATTR void __sf_digitalWrite(int pin, int value) {
          digitalWrite(pin, value);
          ets_printf("SF_EVENT {\"pin\":\"GPIO%d\",\"value\":\"%s\"}\n", pin, value ? "HIGH" : "LOW");
        }
        #define digitalWrite(pin, val) __sf_digitalWrite(pin, val)

        const int __sfLedPin = 2;
        const int __sfBtnPin = 15;

        void setup() {
          pinMode(__sfLedPin, OUTPUT);
          pinMode(__sfBtnPin, INPUT_PULLUP);
          Serial.begin(115200);
          Serial.println("boot");
        }

        void loop() {
          digitalWrite(__sfLedPin, HIGH);
          Serial.println("tick");
          int buttonValue = digitalRead(__sfBtnPin);
          int analogValue = analogRead(34);
          Serial.print(buttonValue);
          Serial.print(analogValue);
          delay(500);
          digitalWrite(__sfLedPin, LOW);
          delay(500);
        }
        """;

    private static readonly SemaphoreSlim GoldenBuildLock = new(1, 1);
    private static volatile bool _goldenBuildReady;

    private readonly StemDbContext _context;
    private readonly IConfiguration _configuration;

    public SimulationCompileService(StemDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // Semaphore CHIA SẺ toàn tiến trình, khởi tạo 1 LẦN DUY NHẤT (double-checked
    // locking) dựa trên config đọc được ở lần gọi ĐẦU TIÊN — vì kích thước 1
    // SemaphoreSlim không đổi được sau khi tạo, đổi config sau đó cần restart
    // process mới có tác dụng (chấp nhận được, cùng mức "cấu hình lúc khởi động"
    // như hầu hết giới hạn tài nguyên khác trong hệ thống này).
    private SemaphoreSlim GetCompileGate()
    {
        if (_compileGate != null)
        {
            return _compileGate;
        }

        lock (CompileGateLock)
        {
            _compileGate ??= new SemaphoreSlim(
                int.TryParse(_configuration["SimulationCompile:MaxConcurrentCompiles"], out var configured) && configured > 0
                    ? configured
                    : DefaultMaxConcurrentCompiles);
        }

        return _compileGate;
    }

    public async Task<CompileSimulationResponse> CompileAsync(
        CompileSimulationRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        Jobs[jobId] = new CompileJobResponse
        {
            JobId = jobId,
            Status = "running",
            CreatedAt = now,
            UpdatedAt = now
        };

        var result = await CompileCoreAsync(request, jobId, cancellationToken);
        Jobs[jobId] = new CompileJobResponse
        {
            JobId = jobId,
            Status = result.Success ? "completed" : "failed",
            Result = result,
            CreatedAt = now,
            UpdatedAt = DateTime.UtcNow
        };

        return result;
    }

    public Task<CompileJobResponse?> GetJobAsync(
        string jobId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        Jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    private async Task<CompileSimulationResponse> CompileCoreAsync(
        CompileSimulationRequest request,
        string jobId,
        CancellationToken cancellationToken)
    {
        var validationErrors = await ValidateRequestAsync(request, cancellationToken);
        if (validationErrors.Count > 0)
        {
            return new CompileSimulationResponse
            {
                Success = false,
                JobId = jobId,
                Errors = validationErrors
            };
        }

        var dockerCliPath = _configuration["SimulationCompile:DockerCliPath"] ?? "docker";
        var dockerImage = _configuration["SimulationCompile:DockerImage"] ?? "stem-arduino-cli-sandbox:latest";
        // 1536m/90s are measured, not guessed: a cold compile of a trivial Blink
        // sketch (--cpus 1.0, fresh tmpfs, no cross-request build cache) genuinely
        // peaked near 945MB RSS and took 39s wall-clock. The old 512m/60s-clamped
        // defaults OOM-killed and would timeout-kill that same legitimate compile.
        var memoryLimit = _configuration["SimulationCompile:MemoryLimit"] ?? "1536m";
        var cpuLimit = _configuration["SimulationCompile:CpuLimit"] ?? "1.0";
        var pidsLimit = _configuration["SimulationCompile:PidsLimit"] ?? "128";
        var buildTmpfsSize = _configuration["SimulationCompile:BuildTmpfsSizeMb"] ?? "256";
        var timeoutSeconds = int.TryParse(_configuration["SimulationCompile:TimeoutSeconds"], out var parsedTimeout)
            ? Math.Clamp(parsedTimeout, 1, 120)
            : 90;
        var workingRoot = _configuration["SimulationCompile:WorkingDirectory"];
        if (string.IsNullOrWhiteSpace(workingRoot))
        {
            workingRoot = Path.Combine(Path.GetTempPath(), "stem-simulation-compile");
        }

        var jobRoot = Path.Combine(workingRoot, jobId);
        var sketchDir = Path.Combine(jobRoot, "sketch");
        var outputDir = Path.Combine(jobRoot, "out");
        var containerName = $"stem-compile-{jobId}";
        var boardFqbn = NormalizeBoard(request.Board);

        // Cache incremental build theo project (VIỆC B — đo thật: core framework
        // ESP32 chiếm ~86% (~33.5s/~39s) thời gian mỗi lần compile, gần như không
        // đổi giữa các lần compile CÙNG 1 project. build-path bền vững (không phải
        // tmpfs mới mỗi lần) cho arduino-cli tái dùng object file core đã build —
        // đo thật: 52.77s (cold) → 8.72s (warm, sketch khác hoàn toàn) — nhanh hơn
        // 83%, đã verify output đúng nội dung sketch mới (không lẫn cache cũ).
        // CHỈ bật khi request.ProjectId có giá trị (xem comment trên field đó) —
        // rỗng thì giữ nguyên hành vi cũ 100% (tmpfs, không cache).
        var buildCacheDir = TryResolveBuildCacheDir(workingRoot, request.ProjectId);
        if (buildCacheDir != null)
        {
            var isNewBuildCache = !Directory.Exists(buildCacheDir);

            // Cache CŨ từ toolchain KHÁC (không có marker, hoặc marker không khớp
            // ToolchainVersion hiện tại) — xoá sạch, coi như build-cache mới hoàn
            // toàn thay vì để arduino-cli lặng lẽ tái dùng .o không tương thích.
            if (!isNewBuildCache && !HasCurrentToolchainMarker(buildCacheDir))
            {
                TryDelete(buildCacheDir);
                isNewBuildCache = true;
            }

            Directory.CreateDirectory(buildCacheDir);
            WriteToolchainMarker(buildCacheDir);
            TryCleanupStaleBuildCaches(workingRoot, exceptDir: buildCacheDir);

            // Seed build-cache MỚI (project/scope chưa từng compile lần nào) bằng
            // "golden" build-path đã warm-up sẵn — đo thật: giảm cold-compile từ
            // ~37s xuống ~20s (core-compile phase 31.37s → ~10s) NGAY CẢ với sketch
            // hoàn toàn khác nội dung, vì phần lớn object file core framework
            // không phụ thuộc sketch cụ thể. Chỉ áp dụng cho ESP32 (board duy nhất
            // QEMU dùng tới) — AVR core hoàn toàn khác, seed nhầm chỉ tốn I/O vô ích.
            if (isNewBuildCache && boardFqbn.Contains("esp32", StringComparison.OrdinalIgnoreCase))
            {
                var goldenDir = Path.Combine(workingRoot, "golden-build-cache");
                await EnsureGoldenBuildCacheAsync(goldenDir, dockerCliPath, dockerImage, cancellationToken);
                if (Directory.Exists(goldenDir) && Directory.EnumerateFileSystemEntries(goldenDir).Any())
                {
                    TryCopyDirectory(goldenDir, buildCacheDir);
                }
            }
        }

        // Giới hạn số lần compile chạy ĐỒNG THỜI trên toàn tiến trình (không phải
        // per-request) — tránh nhiều container arduino-cli (peak ~945MB RAM/cái đã
        // đo thật) cùng chạy làm cạn tài nguyên máy chủ khi nhiều học sinh compile
        // cùng lúc. Semaphore CHIA SẺ static — SimulationCompileService là Scoped
        // (1 instance/request), nếu để instance field thì mỗi request có gate
        // riêng, không giới hạn được gì thật.
        var compileGate = GetCompileGate();
        await compileGate.WaitAsync(cancellationToken);

        try
        {
            Directory.CreateDirectory(sketchDir);
            Directory.CreateDirectory(outputDir);
            var sourceCode = GetSourceCode(request);
            // Encoding.UTF8 (the static property) emits a BOM by default, which gcc
            // rejects as a stray token before the first real line ("'U0000feffvoid'
            // does not name a type") — confirmed via a real compile through the API.
            // UTF8Encoding(false) writes UTF-8 without the BOM preamble.
            await File.WriteAllTextAsync(Path.Combine(sketchDir, "sketch.ino"), sourceCode, new UTF8Encoding(false), cancellationToken);

            // Sandboxing: the only host filesystem the container ever sees is this
            // job's own temp dir (sketch input read-only, output read-write), and the
            // whole dir is deleted in the `finally` below regardless of outcome — both
            // are within the explicitly-allowed "own temp working directory" exception.
            // Build *scratch* (intermediate object files, never needs retrieval) lives
            // on a size-capped tmpfs inside the disposable container and never touches
            // host disk. Output is a host bind mount rather than tmpfs: tmpfs contents
            // are torn down the instant the container exits, so `docker cp` afterward
            // cannot see them ("Could not find the file ... in container" — confirmed
            // via direct repro) — bind-mounting output sidesteps that race entirely.
            // `--network none` cuts all network access for the container.
            // `--memory`/`--cpus`/`--pids-limit` are hard kernel-enforced limits (not
            // best-effort), and `--read-only` plus `--cap-drop ALL` mean nothing outside
            // the tmpfs/output mounts is writable even if a limit is missed.
            using var process = new Process();
            process.StartInfo.FileName = dockerCliPath;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            var args = process.StartInfo.ArgumentList;
            args.Add("run");
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
            args.Add("-v"); args.Add($"{sketchDir}:/workspace/sketch:ro");
            args.Add("-v"); args.Add($"{outputDir}:/workspace/output:rw");
            if (buildCacheDir != null)
            {
                // Bind mount bền vững (không phải tmpfs) — arduino-cli tự phát hiện
                // object file core đã build sẵn trong build-path và bỏ qua bước
                // biên dịch lại (mặc định, không cần cờ riêng — chỉ cần KHÔNG
                // truyền --clean). Không xoá thư mục này trong `finally` bên dưới
                // (khác sketchDir/outputDir) — đây chính là cache, phải sống sót
                // qua nhiều lần compile.
                args.Add("-v"); args.Add($"{buildCacheDir}:/workspace/build:rw");
            }
            else
            {
                args.Add("--tmpfs"); args.Add($"/workspace/build:rw,size={buildTmpfsSize}m,mode=1777");
            }
            // exec is required here: arduino-cli shells out to PyInstaller-packaged
            // tools (esptool et al.) that self-extract into $TMPDIR at runtime and
            // dlopen a bundled libpython .so from there — Docker's --tmpfs defaults
            // to noexec, which makes that dlopen fail (confirmed via direct repro:
            // "failed to map segment from shared object"). Measured real extracted
            // size for esptool is ~10MB; 128m leaves headroom for multiple/concurrent
            // PyInstaller tool invocations in one compile.
            args.Add("--tmpfs"); args.Add("/tmp:rw,exec,size=128m,mode=1777");
            args.Add(dockerImage);
            args.Add("compile");
            args.Add("--fqbn"); args.Add(boardFqbn);
            args.Add("--build-path"); args.Add("/workspace/build/tmp");
            args.Add("--output-dir"); args.Add("/workspace/output");
            args.Add("/workspace/sketch");

            try
            {
                process.Start();
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
            {
                return BuildServiceUnavailable(jobId, dockerCliPath);
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var exited = await WaitForExitAsync(process, TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);

            if (!exited)
            {
                // Second line of defense: killing the `docker run` client process does
                // NOT stop the container in the daemon, so the container must be
                // killed explicitly or the runaway build keeps consuming its
                // (resource-limited, but still real) container until GC in `finally`.
                await TryDockerKillAsync(dockerCliPath, containerName);
                TryKill(process);
                var timeoutStdout = await stdoutTask;
                var timeoutStderr = await stderrTask;
                var timeoutOutput = string.Join(Environment.NewLine, new[] { timeoutStdout, timeoutStderr }.Where(value => !string.IsNullOrWhiteSpace(value)));
                return new CompileSimulationResponse
                {
                    Success = false,
                    JobId = jobId,
                    CompilerOutput = timeoutOutput,
                    Errors = new[]
                    {
                        new CompileSimulationError { Message = $"Compile timed out after {timeoutSeconds} seconds." }
                    }
                };
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var compilerOutput = string.Join(Environment.NewLine, new[] { stdout, stderr }.Where(value => !string.IsNullOrWhiteSpace(value)));

            // Output lands directly in outputDir via the bind mount above — no
            // docker cp step needed (and none would work reliably after exit; see
            // the mount-setup comment above).
            if (process.ExitCode != 0)
            {
                return new CompileSimulationResponse
                {
                    Success = false,
                    JobId = jobId,
                    CompilerOutput = compilerOutput,
                    Errors = ParseCompilerErrors(compilerOutput)
                };
            }

            // esp32:esp32@2.0.17 (pinned — see comment on the core install RUN line in
            // docker/simulation-compile-sandbox/Dockerfile for why) has NO merge_bin
            // post-build step at all (that convenience is 3.x/IDF5-only) — it only ever
            // emits separate bootloader/partitions/app .bin files, never a
            // "*.merged.bin". Without a merged flash image, QEMU's
            // `-drive if=mtd,format=raw` (a flat byte-for-byte flash dump) has no
            // bootloader at the boot offset and never boots. Synthesize one the same
            // way `esptool.py merge_bin` would, at fixed standard ESP32 Arduino
            // offsets — verified real (not guessed): direct QEMU boot of a manually
            // assembled image this way ran clean for 3 independent 12s sessions with
            // continuous GPIO toggle output, no crash (see VIRTUAL_LAB_PLAN.md 8.13).
            TryAssembleMergedEsp32Image(outputDir);

            var candidateFirmwareFiles = Directory
                .EnumerateFiles(outputDir, "*.*", SearchOption.AllDirectories)
                .Where(path =>
                {
                    var extension = Path.GetExtension(path);
                    return extension.Equals(".bin", StringComparison.OrdinalIgnoreCase) ||
                           extension.Equals(".hex", StringComparison.OrdinalIgnoreCase) ||
                           extension.Equals(".elf", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            // ESP32 builds emit several same-extension .bin files at once (app-only,
            // bootloader-only, partition-table-only, and the merged flash image) —
            // "most recently written .bin" is not reliable (confirmed via real
            // tick-precision timestamps: partitions.bin can legitimately be written
            // AFTER merged.bin, same second). "*.merged.bin" is the only one that's
            // actually flashable/bootable (bootloader+partitions+app already placed
            // at the correct offsets) — always prefer it by NAME when present, never
            // by timestamp. Boards with no merge step (e.g. AVR/Uno, which only ever
            // produce .hex/.elf) fall back to the original extension+recency logic
            // unchanged.
            var firmwarePath = candidateFirmwareFiles
                .FirstOrDefault(path => Path.GetFileName(path).EndsWith(".merged.bin", StringComparison.OrdinalIgnoreCase))
                ?? candidateFirmwareFiles
                    .OrderBy(path => FirmwareSortOrder(Path.GetExtension(path)))
                    .ThenByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();

            if (firmwarePath == null)
            {
                return new CompileSimulationResponse
                {
                    Success = false,
                    JobId = jobId,
                    CompilerOutput = compilerOutput,
                    Errors = new[]
                    {
                        new CompileSimulationError { Message = "Compile completed but no firmware artifact (.bin/.hex/.elf) was produced." }
                    }
                };
            }

            var firmwareBytes = await File.ReadAllBytesAsync(firmwarePath, cancellationToken);
            var firmwareBase64 = Convert.ToBase64String(firmwareBytes);
            var firmwareFormat = Path.GetExtension(firmwarePath).TrimStart('.').ToLowerInvariant();
            return new CompileSimulationResponse
            {
                Success = true,
                JobId = jobId,
                HexBase64 = firmwareFormat == "hex" ? firmwareBase64 : null,
                FirmwareBase64 = firmwareBase64,
                FirmwareFileName = Path.GetFileName(firmwarePath),
                FirmwareFormat = firmwareFormat,
                CompilerOutput = compilerOutput,
                Errors = Array.Empty<CompileSimulationError>()
            };
        }
        finally
        {
            await TryDockerRemoveAsync(dockerCliPath, containerName);
            TryDelete(jobRoot);
            compileGate.Release();
        }
    }

    private static async Task TryDockerKillAsync(string dockerCliPath, string containerName)
    {
        await RunDockerCommandBestEffortAsync(dockerCliPath, new[] { "kill", containerName });
    }

    private static async Task TryDockerRemoveAsync(string dockerCliPath, string containerName)
    {
        await RunDockerCommandBestEffortAsync(dockerCliPath, new[] { "rm", "-f", containerName });
    }

    private static async Task RunDockerCommandBestEffortAsync(string dockerCliPath, IEnumerable<string> arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = dockerCliPath;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            process.Start();
            await process.StandardOutput.ReadToEndAsync();
            await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
        }
        catch
        {
            // Best-effort cleanup/copy helper: failures here must never mask the
            // actual compile result, and are covered by TryDelete(jobRoot) + the
            // container's own lifecycle if `docker rm` itself fails.
        }
    }

    private async Task<List<CompileSimulationError>> ValidateRequestAsync(
        CompileSimulationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<CompileSimulationError>();

        // Accept both raw aliases ("esp32_devkit_v1") AND already-normalized FQBNs
        // ("esp32:esp32:esp32:FlashMode=dio") as valid. Some VirtualLabProject rows
        // were persisted with the normalized value already baked into Board (created
        // via POST /api/virtual-lab/projects with the FQBN passed directly, bypassing
        // the alias-only path used by the real Sandbox auto-create flow) — rejecting
        // those here blocks compile for every request that re-uses project.Board as-is.
        var isSupported = SupportedBoards.ContainsKey(request.Board)
            || SupportedBoards.Values.Contains(request.Board, StringComparer.OrdinalIgnoreCase);
        if (!isSupported)
        {
            errors.Add(new CompileSimulationError
            {
                Message = $"Unsupported board '{request.Board}'. Supported boards: {string.Join(", ", SupportedBoards.Keys)}."
            });
        }

        var sourceCode = GetSourceCode(request);
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            errors.Add(new CompileSimulationError { Message = "Source code is required." });
        }
        else if (sourceCode.Length > MaxCodeLength)
        {
            errors.Add(new CompileSimulationError { Message = $"Code is too large. Max length is {MaxCodeLength} characters." });
        }

        if (request.LabId.HasValue)
        {
            var lab = await _context.Labs
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.LabId.Value, cancellationToken);
            if (lab == null)
            {
                errors.Add(new CompileSimulationError { Message = "Lab not found." });
            }
            else if (lab.SimulationMode != LabSimulationModes.CustomSandbox)
            {
                errors.Add(new CompileSimulationError { Message = "Compile is only available for custom_sandbox labs." });
            }
        }

        if (request.AssignmentId.HasValue)
        {
            var assignment = await _context.Assignments
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == request.AssignmentId.Value, cancellationToken);
            if (assignment == null)
            {
                errors.Add(new CompileSimulationError { Message = "Assignment not found." });
            }
            else if (assignment.AssignmentType != AssignmentTypes.PracticalSimulation)
            {
                errors.Add(new CompileSimulationError { Message = "Compile is only available for practical simulation assignments." });
            }
        }

        return errors;
    }

    private static string GetSourceCode(CompileSimulationRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.SourceCode)
            ? request.SourceCode
            : request.Code;
    }

    internal static string NormalizeBoard(string board)
    {
        return SupportedBoards.TryGetValue(board, out var normalized)
            ? normalized
            : board;
    }

    private static int FirmwareSortOrder(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".bin" => 0,
            ".hex" => 1,
            ".elf" => 2,
            _ => 3
        };
    }

    private static async Task<bool> WaitForExitAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var waitTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(timeout, cancellationToken);
        return await Task.WhenAny(waitTask, timeoutTask) == waitTask;
    }

    private static CompileSimulationResponse BuildServiceUnavailable(string jobId, string dockerCliPath)
    {
        return new CompileSimulationResponse
        {
            Success = false,
            JobId = jobId,
            Errors = new[]
            {
                new CompileSimulationError
                {
                    Message = $"Docker is not configured or not found at '{dockerCliPath}'."
                }
            }
        };
    }

    private static IReadOnlyCollection<CompileSimulationError> ParseCompilerErrors(string compilerOutput)
    {
        var errors = compilerOutput
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                var match = LineErrorRegex.Match(line);
                return match.Success
                    ? new CompileSimulationError
                    {
                        Line = int.Parse(match.Groups["line"].Value),
                        Message = match.Groups["message"].Value.Trim()
                    }
                    : null;
            })
            .Where(error => error != null)
            .Cast<CompileSimulationError>()
            .ToList();

        return errors.Count > 0
            ? errors
            : new[] { new CompileSimulationError { Message = string.IsNullOrWhiteSpace(compilerOutput) ? "Compile failed." : compilerOutput } };
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }

    private static void TryDelete(string path)
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

    // Lazy warm-up, double-checked lock — chỉ 1 compile "vàng" chạy trong suốt
    // vòng đời tiến trình (không phải mỗi lần server restart lại tốn ~50s chờ
    // request đầu tiên; ngược lại, request đầu tiên VẪN phải đợi nếu golden
    // cache chưa có, chấp nhận được vì chỉ xảy ra đúng 1 lần). Golden dùng
    // build-path RIÊNG, KHÔNG trùng với build-cache của bất kỳ project nào —
    // không có compile thật nào khác từng ghi đồng thời vào chính golden này
    // (chỉ đọc lại qua TryCopyDirectory), nên không lặp lại được bug lẫn dữ
    // liệu đã phát hiện khi 2 compile GHI chung 1 thư mục.
    private async Task EnsureGoldenBuildCacheAsync(
        string goldenDir,
        string dockerCliPath,
        string dockerImage,
        CancellationToken cancellationToken)
    {
        if (_goldenBuildReady && Directory.Exists(goldenDir) && Directory.EnumerateFileSystemEntries(goldenDir).Any())
        {
            return;
        }

        await GoldenBuildLock.WaitAsync(cancellationToken);
        try
        {
            if (_goldenBuildReady && Directory.Exists(goldenDir) && Directory.EnumerateFileSystemEntries(goldenDir).Any())
            {
                return;
            }

            // Golden cache TRÊN ĐĨA sống sót qua nhiều lần process restart (khác
            // _goldenBuildReady, chỉ là biến in-memory reset mỗi lần restart) — nếu
            // thư mục đã tồn tại từ TRƯỚC lần đổi core/toolchain gần nhất (không có
            // marker khớp), object file .o bên trong CŨ, KHÔNG được coi là "đã sẵn
            // sàng" dù tồn tại — phải xoá sạch build lại, nếu không mọi project MỚI
            // (seed từ golden này) đều dính lẫn ABI cũ/mới. Xem VIRTUAL_LAB_PLAN.md 8.16.
            if (Directory.Exists(goldenDir) && !HasCurrentToolchainMarker(goldenDir))
            {
                TryDelete(goldenDir);
            }

            var warmupRoot = Path.Combine(Path.GetTempPath(), "stem-golden-warmup-" + Guid.NewGuid().ToString("N"));
            var sketchDir = Path.Combine(warmupRoot, "sketch");
            var outputDir = Path.Combine(warmupRoot, "out");

            try
            {
                Directory.CreateDirectory(sketchDir);
                Directory.CreateDirectory(outputDir);
                Directory.CreateDirectory(goldenDir);
                await File.WriteAllTextAsync(
                    Path.Combine(sketchDir, "sketch.ino"),
                    GoldenSketchSource,
                    new UTF8Encoding(false),
                    cancellationToken);

                using var process = new Process();
                process.StartInfo.FileName = dockerCliPath;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                var args = process.StartInfo.ArgumentList;
                args.Add("run");
                args.Add("--rm");
                args.Add("--name"); args.Add("stem-golden-warmup-" + Guid.NewGuid().ToString("N"));
                args.Add("--network"); args.Add("none");
                args.Add("--memory"); args.Add("1536m");
                args.Add("--memory-swap"); args.Add("1536m");
                args.Add("--cpus"); args.Add("1.0");
                args.Add("--pids-limit"); args.Add("128");
                args.Add("--cap-drop"); args.Add("ALL");
                args.Add("--security-opt"); args.Add("no-new-privileges");
                args.Add("--read-only");
                args.Add("--user"); args.Add("10001:10001");
                args.Add("-v"); args.Add($"{sketchDir}:/workspace/sketch:ro");
                args.Add("-v"); args.Add($"{outputDir}:/workspace/output:rw");
                args.Add("-v"); args.Add($"{goldenDir}:/workspace/build:rw");
                args.Add("--tmpfs"); args.Add("/tmp:rw,exec,size=128m,mode=1777");
                args.Add(dockerImage);
                args.Add("compile");
                args.Add("--fqbn"); args.Add("esp32:esp32:esp32:FlashMode=dio");
                args.Add("--build-path"); args.Add("/workspace/build");
                args.Add("--output-dir"); args.Add("/workspace/output");
                args.Add("/workspace/sketch");

                process.Start();
                var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
                var exited = await WaitForExitAsync(process, TimeSpan.FromSeconds(120), cancellationToken);

                if (!exited)
                {
                    TryKill(process);
                    _goldenBuildReady = false;
                    return;
                }

                await stdoutTask;
                await stderrTask;
                _goldenBuildReady = process.ExitCode == 0 && Directory.EnumerateFileSystemEntries(goldenDir).Any();
                if (_goldenBuildReady)
                {
                    WriteToolchainMarker(goldenDir);
                }
            }
            catch
            {
                // Warm-up thất bại không được phép làm hỏng compile thật đang chờ —
                // chỉ đơn giản là không có golden cache để seed, compile vẫn tiếp
                // tục bình thường (chậm hơn, không sai).
                _goldenBuildReady = false;
            }
            finally
            {
                TryDelete(warmupRoot);
            }
        }
        finally
        {
            GoldenBuildLock.Release();
        }
    }

    // Chuẩn ESP32 Arduino (partition scheme mặc định, không OTA runtime-select):
    // bootloader @ 0x1000, partition table @ 0x8000, app @ 0x10000, tổng ảnh
    // flash 4MB (0x400000) — đúng offset esptool.py merge_bin dùng, xác nhận
    // qua boot thật trong QEMU (không đoán).
    private const int MergedImageSize = 4 * 1024 * 1024;
    private const int BootloaderOffset = 0x1000;
    private const int PartitionsOffset = 0x8000;
    private const int AppOffset = 0x10000;

    // esp32:esp32@2.0.17 không tự sinh "*.merged.bin" (khác 3.x) — chỉ có
    // {name}.bootloader.bin/{name}.partitions.bin/{name}.bin riêng lẻ. Tự ghép
    // thủ công đúng offset esptool.py merge_bin dùng, ghi ra
    // "{name}.merged.bin" cùng thư mục để logic chọn firmware phía trên
    // (ưu tiên *.merged.bin theo TÊN) tự nhặt được, không cần đổi gì thêm.
    // No-op an toàn nếu thiếu file nào hoặc đã có sẵn merged.bin thật (board
    // khác/core khác tự sinh merged.bin, không ghi đè).
    private static void TryAssembleMergedEsp32Image(string outputDir)
    {
        try
        {
            if (!Directory.Exists(outputDir))
            {
                return;
            }

            var binFiles = Directory.EnumerateFiles(outputDir, "*.bin", SearchOption.AllDirectories).ToList();
            if (binFiles.Any(path => Path.GetFileName(path).EndsWith(".merged.bin", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var bootloaderPath = binFiles.FirstOrDefault(path => Path.GetFileName(path).EndsWith(".bootloader.bin", StringComparison.OrdinalIgnoreCase));
            var partitionsPath = binFiles.FirstOrDefault(path => Path.GetFileName(path).EndsWith(".partitions.bin", StringComparison.OrdinalIgnoreCase));
            var appPath = binFiles.FirstOrDefault(path =>
            {
                var name = Path.GetFileName(path);
                return !name.EndsWith(".bootloader.bin", StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith(".partitions.bin", StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith(".merged.bin", StringComparison.OrdinalIgnoreCase);
            });

            if (bootloaderPath == null || partitionsPath == null || appPath == null)
            {
                // Không phải build ESP32 (vd AVR chỉ có .hex/.elf) hoặc core đời khác
                // đã tự sinh merged.bin rồi — không có gì để ghép.
                return;
            }

            var image = new byte[MergedImageSize];
            Array.Fill(image, (byte)0xFF);
            PlaceAt(image, bootloaderPath, BootloaderOffset);
            PlaceAt(image, partitionsPath, PartitionsOffset);
            PlaceAt(image, appPath, AppOffset);

            var mergedPath = Path.Combine(
                Path.GetDirectoryName(appPath)!,
                Path.GetFileNameWithoutExtension(appPath) + ".merged.bin");
            File.WriteAllBytes(mergedPath, image);
        }
        catch
        {
            // Ghép thất bại không được phép làm hỏng compile thật vừa xong — chỉ
            // đơn giản không có merged.bin, firmwarePath selection phía trên sẽ
            // fallback sang app .bin đơn lẻ (không boot được, báo lỗi rõ ràng
            // hơn là throw ở đây).
        }

        static void PlaceAt(byte[] image, string path, int offset)
        {
            var data = File.ReadAllBytes(path);
            Array.Copy(data, 0, image, offset, data.Length);
        }
    }

    // Copy NÔNG (không theo symlink) toàn bộ cây thư mục — dùng để seed 1
    // build-cache MỚI từ golden, không bao giờ ghi ngược lại golden.
    private static void TryCopyDirectory(string sourceDir, string destinationDir)
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(Path.Combine(destinationDir, Path.GetRelativePath(sourceDir, dir)));
            }

            foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(destinationDir, Path.GetRelativePath(sourceDir, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }
        catch
        {
            // Seed thất bại không được phép làm hỏng compile thật — chỉ đơn giản
            // build-cache của scope này bắt đầu rỗng (chậm hơn như trước khi có
            // golden cache), không sai kết quả.
        }
    }

    // Đọc marker ToolchainVersion đã ghi lần build gần nhất trong thư mục cache
    // (build-cache theo project HOẶC golden-build-cache) — false nếu marker
    // thiếu/không khớp bản hiện tại, nghĩa là cache này build bằng core/toolchain
    // KHÁC (vd 3.3.10 trước khi pin xuống 2.0.17), không được coi là dùng lại
    // được. Xem VIRTUAL_LAB_PLAN.md 8.16.
    private static bool HasCurrentToolchainMarker(string cacheDir)
    {
        try
        {
            var markerPath = Path.Combine(cacheDir, ToolchainVersionMarkerFileName);
            return File.Exists(markerPath) && File.ReadAllText(markerPath).Trim() == ToolchainVersion;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteToolchainMarker(string cacheDir)
    {
        try
        {
            File.WriteAllText(Path.Combine(cacheDir, ToolchainVersionMarkerFileName), ToolchainVersion);
        }
        catch
        {
            // Ghi marker thất bại không được phép làm hỏng compile thật vừa xong —
            // hệ quả xấu nhất là lần sau bị coi nhầm là cache cũ, build lại từ đầu
            // (chậm hơn, không sai).
        }
    }

    // "arduino" prefix tránh đụng độ nếu 1 project GUID nào đó vô tình trùng
    // định dạng với thư mục jobRoot khác trong cùng workingRoot (jobId là
    // Guid ngẫu nhiên mới mỗi lần, projectId ổn định — 2 namespace khác nhau
    // trên cùng workingRoot nếu không tách riêng có thể lẫn nhau khi liệt kê
    // thư mục để dọn dẹp).
    private static string? TryResolveBuildCacheDir(string workingRoot, string? projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return null;
        }

        // Chỉ chấp nhận Guid hợp lệ — projectId đi thẳng vào đường dẫn thư mục
        // trên host, không được tin bất kỳ chuỗi tuỳ ý nào (path traversal).
        if (!Guid.TryParse(projectId, out var id))
        {
            return null;
        }

        return Path.Combine(workingRoot, "build-cache", id.ToString("N"));
    }

    // Dọn cache cũ CƠ HỘI (không cần job/cron riêng) — chạy ngay trước mỗi lần
    // compile có bật cache, chi phí thấp (chỉ liệt kê + so LastWriteTimeUtc của
    // các thư mục con, KHÔNG quét nội dung bên trong). Retention mặc định 7
    // ngày — đủ dài cho 1 học sinh quay lại làm tiếp bài cũ, đủ ngắn để không
    // phình đĩa vô hạn qua nhiều lớp/nhiều kỳ học.
    private static void TryCleanupStaleBuildCaches(string workingRoot, string exceptDir)
    {
        try
        {
            var cacheRoot = Path.Combine(workingRoot, "build-cache");
            if (!Directory.Exists(cacheRoot))
            {
                return;
            }

            var retentionDays = 7;
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

            foreach (var dir in Directory.EnumerateDirectories(cacheRoot))
            {
                if (string.Equals(dir, exceptDir, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Directory.GetLastWriteTimeUtc(dir) < cutoff)
                {
                    TryDelete(dir);
                }
            }
        }
        catch
        {
            // Best-effort — dọn cache thất bại không được phép làm hỏng lần
            // compile đang chạy.
        }
    }
}
