using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;
using STEM.Infrastructure.Services;

namespace STEM.Application.Tests;

// TIẾP TỤC FINAL MANUAL E2E VERIFICATION (2026-08-25). Locks in, with a real
// automated test, the diagram-component-aware routing override added to
// VirtualLabRuntimeService.RunEsp32Async this same task (found live via
// manual browser testing — see git history/commit message for the full
// story). Before this fix, ResolveSimulationMode() was a flat global config
// read with NO component-awareness at all; this test exists specifically so
// that fact can never silently regress again — in particular, the live
// browser session for this task explicitly required proving Robot Delivery
// (L298N/HC-SR04-based labs) is NEVER accidentally routed to "educational"
// by the fix meant only for simple Button/Potentiometer/Light-Sensor labs.
//
// Uses a RecordingRunnerResolver fake that only records which mode string
// reached ISimulationRunnerResolver.Resolve(...) and returns a no-op runner
// (Success=true, no events) — this test is about the ROUTING DECISION itself,
// not about exercising a real Educational/QEMU runner (those are already
// covered elsewhere: RealtimeSimulationInputTests.cs for Educational,
// RobotDeliveryQemuIntegrationTests.cs for real QEMU).
public sealed class SimulationModeRoutingTests
{
    private sealed class RecordingRunnerResolver : ISimulationRunnerResolver
    {
        public string? LastResolvedMode { get; private set; }

        public ISimulationRunner Resolve(string mode)
        {
            LastResolvedMode = mode;
            return new NoOpRunner();
        }

        private sealed class NoOpRunner : ISimulationRunner
        {
            public Task<SimulationRunResult> RunAsync(SimulationRunContext context, CancellationToken cancellationToken) =>
                Task.FromResult(new SimulationRunResult { Success = true });
        }
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
        public Task<IEnumerable<STEM.Core.Entities.Common.Notification>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<STEM.Core.Entities.Common.Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<STEM.Core.Entities.Common.Notification>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<STEM.Core.Entities.Common.Notification>> FindAsync(System.Linq.Expressions.Expression<Func<STEM.Core.Entities.Common.Notification, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(STEM.Core.Entities.Common.Notification entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddRangeAsync(IEnumerable<STEM.Core.Entities.Common.Notification> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Update(STEM.Core.Entities.Common.Notification entity) => throw new NotSupportedException();
        public void Delete(STEM.Core.Entities.Common.Notification entity) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(STEM.Core.Entities.Common.Notification entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static (VirtualLabRuntimeService Service, RecordingRunnerResolver Resolver) CreateService()
    {
        var dbOptions = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"routing-test-{Guid.NewGuid():N}")
            .Options;
        var context = new StemDbContext(dbOptions);

        var configuration = new ConfigurationManager();
        // Mirrors the DEPLOYED appsettings.json value exactly — this is the
        // whole point: prove the override works against the real production
        // default, not a config value chosen to make the test pass.
        configuration["SimulationRunner:DefaultMode"] = "qemu";

        var resolver = new RecordingRunnerResolver();

        var service = new VirtualLabRuntimeService(
            context,
            new VirtualLabDiagramService(),
            resolver,
            new ThrowingCompileService(),
            configuration,
            new RunningSimulationRegistry(),
            new ThrowingPrecompileTrigger(),
            new SystemDateTimeProvider(),
            new ThrowingNotificationRepository());

        return (service, resolver);
    }

    [Theory]
    [InlineData("wokwi-pushbutton", "1.l")]
    [InlineData("wokwi-potentiometer", "SIG")]
    [InlineData("wokwi-photoresistor-sensor", "AO")]
    public async Task SimpleEducationalOnlyDiagram_RoutesTo_Educational_DespiteQemuDefault(string componentType, string signalPin)
    {
        var (service, resolver) = CreateService();

        var diagram = $$"""
        {
          "board": "esp32_devkit_v1",
          "parts": [ { "id": "c1", "type": "{{componentType}}" }, { "id": "led1", "type": "wokwi-led" } ],
          "connections": [
            ["arduino:GPIO27", "c1:{{signalPin}}"],
            ["arduino:GPIO13", "led1:A"],
            ["led1:C", "arduino:GND.1"]
          ]
        }
        """;

        await service.RunEsp32Async(new RunEsp32SimulationRequest
        {
            DiagramJson = diagram,
            SourceCode = "void setup(){} void loop(){}",
        }, currentUserId: null, CancellationToken.None);

        Assert.Equal("educational", resolver.LastResolvedMode);
    }

    [Fact]
    public async Task RobotDeliveryDiagram_L298nAndHcSr04_StaysOnQemu_NotAccidentallyEducational()
    {
        var (service, resolver) = CreateService();

        // Real LAB06/08-shaped diagram (L298N + HC-SR04 + motors + battery) —
        // the exact case the task explicitly warned must NOT flip to
        // educational as a side effect of the button/pot/light-sensor fix.
        const string diagram = """
        {
          "board": "esp32_devkit_v1",
          "parts": [
            { "id": "l298n1", "type": "wokwi-l298n" },
            { "id": "motorL", "type": "wokwi-dc-motor" },
            { "id": "motorR", "type": "wokwi-dc-motor" },
            { "id": "battery1", "type": "wokwi-battery-pack" },
            { "id": "us1", "type": "wokwi-hc-sr04" }
          ],
          "connections": [
            ["arduino:GPIO13", "l298n1:IN1"], ["arduino:GPIO14", "l298n1:IN2"],
            ["arduino:GPIO16", "l298n1:IN3"], ["arduino:GPIO17", "l298n1:IN4"],
            ["motorL:terminal1", "l298n1:OUT1"], ["motorL:terminal2", "l298n1:OUT2"],
            ["motorR:terminal1", "l298n1:OUT3"], ["motorR:terminal2", "l298n1:OUT4"],
            ["battery1:+", "l298n1:VIN"], ["battery1:-", "l298n1:GND"], ["l298n1:GND", "arduino:GND.1"],
            ["arduino:3V3", "us1:VCC"], ["arduino:GPIO32", "us1:TRIG"], ["arduino:GPIO33", "us1:ECHO"], ["us1:GND", "arduino:GND.1"]
          ]
        }
        """;

        await service.RunEsp32Async(new RunEsp32SimulationRequest
        {
            DiagramJson = diagram,
            SourceCode = "void setup(){} void loop(){}",
        }, currentUserId: null, CancellationToken.None);

        Assert.Equal("qemu", resolver.LastResolvedMode);
    }

    [Fact]
    public async Task DhtDiagram_StaysOnQemu_NotEducational()
    {
        var (service, resolver) = CreateService();

        const string diagram = """
        {
          "board": "esp32_devkit_v1",
          "parts": [ { "id": "dht1", "type": "wokwi-dht11" } ],
          "connections": [
            ["arduino:GPIO19", "dht1:SDA"],
            ["arduino:3V3", "dht1:VCC"],
            ["dht1:GND", "arduino:GND.1"]
          ]
        }
        """;

        await service.RunEsp32Async(new RunEsp32SimulationRequest
        {
            DiagramJson = diagram,
            SourceCode = "#include \"StemFlowDHT.h\"\nvoid setup(){} void loop(){}",
        }, currentUserId: null, CancellationToken.None);

        Assert.Equal("qemu", resolver.LastResolvedMode);
    }

    [Fact]
    public async Task MixedDiagram_EducationalComponentPlusL298n_StaysOnQemu()
    {
        // A button sharing a diagram with an L298N (e.g. a robot with a
        // manual override button) must NOT flip to educational just because
        // SOME components are educational-modeled — the override only fires
        // when EVERY component is educational-safe.
        var (service, resolver) = CreateService();

        const string diagram = """
        {
          "board": "esp32_devkit_v1",
          "parts": [
            { "id": "button1", "type": "wokwi-pushbutton" },
            { "id": "l298n1", "type": "wokwi-l298n" },
            { "id": "motorL", "type": "wokwi-dc-motor" }
          ],
          "connections": [
            ["arduino:GPIO27", "button1:1.l"], ["button1:2.r", "arduino:GND.1"],
            ["arduino:GPIO13", "l298n1:IN1"], ["arduino:GPIO14", "l298n1:IN2"],
            ["motorL:terminal1", "l298n1:OUT1"], ["motorL:terminal2", "l298n1:OUT2"]
          ]
        }
        """;

        await service.RunEsp32Async(new RunEsp32SimulationRequest
        {
            DiagramJson = diagram,
            SourceCode = "void setup(){} void loop(){}",
        }, currentUserId: null, CancellationToken.None);

        Assert.Equal("qemu", resolver.LastResolvedMode);
    }
}
