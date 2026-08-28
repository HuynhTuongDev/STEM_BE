using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Simulations;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;
using STEM.Infrastructure.Services;

namespace STEM.Application.Tests;

// CLOSE REMAINING QEMU RUNTIME STABILITY GAPS task (2026-08-28) — PHASE L.
// Regression tests for BUG B (zombie stem-qemu-* container on rapid
// Run→Stop→Run) whose confirmed root cause was RunningSimulationRegistry.
// Register() cancelling the PREVIOUS session's CTS but never awaiting its
// background task's own cleanup (kill process, docker rm -f, finally block)
// before the caller proceeds — verified live via `docker ps` showing a
// container "Up 15+ minutes" well after Register() had already been called
// again for the same project. Fixed via IRunningSimulationRegistry.
// RegisterAsync (cancel + AWAIT previous cleanup + start new session, all
// serialized per-projectId under a real SemaphoreSlim — no Task.Delay).
//
// These tests exercise the REAL RunningSimulationRegistry + REAL
// EducationalSimulationRunner (same production wiring
// SimulationRunnerResolverTests.CreateStreamingRunner already uses) — no
// Docker required (Educational mode holds no OS resource), so they run in
// every normal `dotnet test` pass, unlike RobotDeliveryQemuIntegrationTests.
// Every assertion is on OBSERVABLE lifecycle state (registry.IsRunning,
// broadcaster.FinalStatus, whether a background task's own completion
// side-effects have already run) — never "was method X called".
public sealed class RunningSimulationLifecycleTests
{
    private static readonly string DiagramJsonLedOnPin13 = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-resistor", "id": "r1" },
        { "type": "wokwi-led", "id": "led1" }
      ],
      "connections": [
        [ "esp:GPIO13", "r1:1" ],
        [ "r1:2", "led1:A" ],
        [ "led1:C", "esp:GND.1" ]
      ]
    }
    """;

    private const string BlinkForever = """
        void setup() { pinMode(13, OUTPUT); }
        void loop() { digitalWrite(13, HIGH); delay(1000); digitalWrite(13, LOW); delay(1000); }
        """;

    private static SimulationRunContext BlinkContext(string projectId, int maxDurationMs = 60_000) => new()
    {
        ProjectId = projectId,
        Mode = "educational",
        MaxDurationMs = maxDurationMs,
        MaxInstructionCount = 100_000,
        DiagramJson = DiagramJsonLedOnPin13,
        SourceCode = BlinkForever
    };

    // Test #2 (PHASE L). Trước fix: Register() chỉ Cancel(previous) rồi trả
    // về NGAY — không có gì đảm bảo background task cũ (và container/resource
    // của nó, ở runner QEMU) đã thực sự dừng+dọn xong trước khi lần chạy mới
    // được coi là active. Ở đây verify đúng invariant đó bằng 1 tín hiệu
    // OBSERVABLE không thể giả được: session cũ chỉ đặt broadcaster.FinalStatus
    // = "stopped" ở TRONG finally của chính nó (chạy SAU KHI đã bị cancel) —
    // nếu RunAsync() lần 2 trả về TRƯỚC KHI finally đó chạy xong, FinalStatus
    // vẫn sẽ là null tại thời điểm kiểm tra (đúng bug cũ). Với fix, RunAsync()
    // lần 2 PHẢI await xong toàn bộ cleanup của lần 1 trước khi return.
    [Fact]
    public async Task StartingNewSession_ShouldCancelAndCleanupPreviousSession()
    {
        var (runner, broadcaster, _, registry, _) = SimulationRunnerResolverTests.CreateStreamingRunner();
        var projectId = Guid.NewGuid().ToString("N");

        var firstStart = await runner.RunAsync(BlinkContext(projectId), CancellationToken.None);
        Assert.True(firstStart.Success, string.Join("; ", firstStart.Errors));

        // Để chắc lần chạy 1 thực sự đang "active" (không phải vừa Register
        // xong nhưng background task chưa kịp bắt đầu vòng lặp) trước khi bị
        // thay thế.
        await Task.Delay(300);
        Assert.True(registry.IsRunning(projectId));
        Assert.Null(broadcaster.FinalStatus);

        var secondStart = await runner.RunAsync(BlinkContext(projectId), CancellationToken.None);
        Assert.True(secondStart.Success, string.Join("; ", secondStart.Errors));

        // ĐÂY LÀ ASSERT CHÍNH — pin đúng invariant "cancel + CHỜ dọn xong
        // trước khi session mới active". Với Register() cũ (không await),
        // dòng này flaky-fail (FinalStatus vẫn null, vì lần chạy 1's finally
        // có thể chưa kịp chạy tới BroadcastRunCompletedAsync lúc RunAsync()
        // lần 2 đã trả về).
        Assert.Equal(VirtualLabProjectStatuses.Stopped, broadcaster.FinalStatus);

        // Session mới (lần 2) phải đang active ngay sau đó.
        Assert.True(registry.IsRunning(projectId));
    }

    // Test #3 (PHASE L). Idempotency của Stop thật (VirtualLabRuntimeService.
    // StopSimulationAsync, đúng tầng mà controller /stop gọi) — Run rồi gọi
    // Stop 2 lần liên tiếp không được throw, không tạo completion trùng lặp,
    // registry phải sạch sau cùng.
    [Fact]
    public async Task StopSimulation_ShouldBeIdempotent()
    {
        // registry lấy TỪ tuple trả về của CreateStreamingRunner() — CHIA SẺ
        // đúng 1 instance giữa EducationalSimulationRunner và
        // VirtualLabRuntimeService bên dưới, giống production (Singleton).
        var (runner, broadcaster, _, registry, _) = SimulationRunnerResolverTests.CreateStreamingRunner();

        var dbOptions = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"stop-idempotent-{Guid.NewGuid():N}")
            .Options;
        var context = new StemDbContext(dbOptions);

        var configuration = new ConfigurationManager();
        configuration["SimulationRunner:DefaultMode"] = "educational";

        var service = new VirtualLabRuntimeService(
            context,
            new VirtualLabDiagramService(),
            new SingleRunnerResolver(runner),
            new ThrowingCompileService(),
            configuration,
            registry,
            new ThrowingPrecompileTrigger(),
            new SystemDateTimeProvider(),
            new ThrowingNotificationRepository());

        var projectId = Guid.NewGuid();

        var startResponse = await service.RunEsp32Async(new RunEsp32SimulationRequest
        {
            SessionId = projectId.ToString("N"),
            DiagramJson = DiagramJsonLedOnPin13,
            SourceCode = BlinkForever
        }, currentUserId: null, CancellationToken.None);
        Assert.Equal(VirtualLabProjectStatuses.Running, startResponse.Status);

        await Task.Delay(300);
        Assert.True(registry.IsRunning(projectId.ToString("N")));

        var firstStop = await service.StopSimulationAsync(projectId, currentUserId: 0, CancellationToken.None);
        Assert.True(firstStop);

        // Cho background task đủ thời gian chạy xong finally (Remove khỏi
        // registry, phát RunCompleted) sau khi bị cancel.
        await Task.Delay(300);
        Assert.False(registry.IsRunning(projectId.ToString("N")));
        Assert.Equal(VirtualLabProjectStatuses.Stopped, broadcaster.FinalStatus);

        // Gọi Stop LẦN 2 — project vẫn tồn tại (đã Stopped), registry đã
        // Remove từ trước. Phải KHÔNG throw, KHÔNG tạo completion/broadcast
        // trùng lặp (registry.TryCancel lần 2 chỉ trả false — vô hại).
        var secondStop = await service.StopSimulationAsync(projectId, currentUserId: 0, CancellationToken.None);
        Assert.True(secondStop, "StopSimulationAsync lần 2 vẫn phải trả true (project tồn tại), không throw.");
        Assert.Equal(VirtualLabProjectStatuses.Stopped, broadcaster.FinalStatus);
        Assert.False(registry.IsRunning(projectId.ToString("N")));
    }

    // Test #4 (PHASE L). 2 lệnh Start gần như đồng thời cho CÙNG 1 project —
    // đúng kịch bản PHASE K. Không được để cả 2 session cùng active — đúng
    // invariant "MAX 1 ACTIVE RUNNER" (PHASE H). Registry.RegisterAsync
    // serialize hoá bằng SemaphoreSlim theo projectId nên lệnh thứ 2 phải đợi
    // lệnh thứ 1 (đăng ký xong Task) trước khi tự thay thế nó — không có cửa
    // sổ nào để cả 2 cùng "active" một lúc.
    [Fact]
    public async Task ConcurrentStarts_ShouldNotLeaveMultipleActiveSessions()
    {
        var (runner, broadcaster, _, registry, _) = SimulationRunnerResolverTests.CreateStreamingRunner();
        var projectId = Guid.NewGuid().ToString("N");

        // Task.Run (không gọi RunAsync() trực tiếp rồi await tuần tự) — để 2
        // lệnh Start thực sự có cơ hội chạy trên 2 thread-pool thread khác
        // nhau, tạo đúng race "gần như đồng thời" thay vì vô tình tuần tự hoá
        // (RunAsync() không có await nào trước RegisterAsync() nên gọi thẳng
        // 2 lần liên tiếp không await ở giữa vẫn có thể chạy hết A đồng bộ
        // trước khi B kịp bắt đầu).
        var startA = Task.Run(() => runner.RunAsync(BlinkContext(projectId), CancellationToken.None));
        var startB = Task.Run(() => runner.RunAsync(BlinkContext(projectId), CancellationToken.None));

        var results = await Task.WhenAll(startA, startB);
        Assert.All(results, r => Assert.True(r.Success, string.Join("; ", r.Errors)));

        // Sau khi CẢ 2 lệnh Start đã trả về, đúng 1 session phải đang active
        // cho project này (không phải 0, không phải "2 session cùng active"
        // — điều registry vốn dĩ không biểu diễn được nhiều hơn 1 entry mỗi
        // projectId, nhưng bug thật ở đây là CTS/Task bị ghi đè giữa chừng
        // trong lúc container/Task cũ vẫn còn active, không phải "2 dòng
        // trong dictionary").
        Assert.True(registry.IsRunning(projectId));

        await Task.Delay(500);

        // Cho phiên "thắng" (dù là A hay B) chạy xong tự nhiên qua Stop.
        //
        // KHÔNG được chờ qua broadcaster.Completed ở đây: đó là 1
        // TaskCompletionSource DÙNG 1 LẦN (TrySetResult), và phiên "thua"
        // CHẮC CHẮN đã tự hoàn tất (bị cancel + chạy hết finally, tự
        // BroadcastRunCompletedAsync("stopped")) TỪ TRƯỚC — ngay trong lúc
        // RegisterAsync() của phiên "thắng" `await previous.RunTask` phía
        // trên (trước cả khi Task.WhenAll(startA, startB) return). Nghĩa là
        // broadcaster.Completed đã được set bởi phiên THUA rồi, nên chờ nó ở
        // đây sẽ trả về NGAY LẬP TỨC mà không thật sự đợi phiên THẮNG dừng —
        // khiến Assert.False(IsRunning) bên dưới đua với cleanup của phiên
        // thắng vẫn đang chạy nền (đây là nguyên nhân test này flaky, không
        // phải lỗi ở RunningSimulationRegistry). Poll thẳng trên
        // registry.IsRunning — tín hiệu KHÔNG dùng 1 lần, phản ánh đúng
        // trạng thái thật của phiên đang active bất kể nó là A hay B.
        registry.TryCancel(projectId);
        var stopped = await WaitForNotRunningAsync(registry, projectId);
        Assert.True(stopped, "Phiên đang active không dừng (Remove khỏi registry) trong thời gian chờ.");
        Assert.Equal(VirtualLabProjectStatuses.Stopped, broadcaster.FinalStatus);
        Assert.False(registry.IsRunning(projectId));
    }

    private static async Task<bool> WaitForNotRunningAsync(IRunningSimulationRegistry registry, string projectId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (!registry.IsRunning(projectId))
            {
                return true;
            }

            await Task.Delay(25);
        }

        return !registry.IsRunning(projectId);
    }

    private sealed class SingleRunnerResolver : ISimulationRunnerResolver
    {
        private readonly ISimulationRunner _runner;
        public SingleRunnerResolver(ISimulationRunner runner) => _runner = runner;
        public ISimulationRunner Resolve(string mode) => _runner;
    }

    private sealed class ThrowingCompileService : ISimulationCompileService
    {
        public Task<CompileSimulationResponse> CompileAsync(CompileSimulationRequest request, int currentUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<CompileJobResponse?> GetJobAsync(string jobId, int currentUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ThrowingPrecompileTrigger : IPrecompileTriggerService
    {
        public void TriggerBackgroundCompile(string sourceCode, string board, string framework, Guid? buildCacheScopeId) =>
            throw new NotSupportedException();
    }

    private sealed class ThrowingNotificationRepository : INotificationRepository
    {
        public Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, int skip, int take, NotificationType? type = null, NotificationStatus? status = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<Notification>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<Notification>> FindAsync(System.Linq.Expressions.Expression<Func<Notification, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Notification entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddRangeAsync(IEnumerable<Notification> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Update(Notification entity) => throw new NotSupportedException();
        public void Delete(Notification entity) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Notification entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
