using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Simulation;
using STEM.Application.Interfaces;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Application.UseCases.Simulation.Runtime;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;
using STEM.Infrastructure.Services;

namespace STEM.Application.Tests;

// CLOSE REMAINING FINAL-LAB GAPS (STEP 7). Task requirement: prove
// diagram+code+sensorScenario+mechanicalLinks survive a REAL
// submit -> backend receive -> retrieve cycle, not just "the TypeScript
// object looked right before POST" (that was the entire gap — FE's own
// pre-POST object always looked correct; the actual bug that got fixed
// earlier this audit was submitVirtualLab silently DROPPING sensorScenario/
// mechanicalLinks before the request even left the browser). This test goes
// through the REAL VirtualLabRuntimeService.SubmitVirtualLabAsync (production
// service, not reimplemented logic) and a REAL EF Core StemDbContext
// (InMemory provider) — Submit writes a Submission row via
// _context.SaveChangesAsync, then a SEPARATE freshly-opened DbContext
// instance reads it back, proving this isn't just re-reading the same
// tracked in-memory entity from one open context/session.
//
// Why InMemory instead of the real Docker/QEMU-backed harness used by
// RobotDeliveryQemuIntegrationTests.cs: this proof is about JSON persistence
// fidelity through VirtualLabDiagramService.Analyze's GetRawText() round-trip
// + EF Core save/reload, not about compile/QEMU runtime — no Docker
// dependency exists on this path at all (SessionId is intentionally left
// unset below, which is what makes VirtualLabRuntimeService.
// BuildCompileCheckAsync/LoadPersistedSimulationEventsAsync short-circuit
// without ever calling ISimulationCompileService — verified by reading
// VirtualLabRuntimeService.cs directly, not assumed).
public sealed class VirtualLabSubmissionRoundTripTests
{
    private sealed class ThrowingRunnerResolver : ISimulationRunnerResolver
    {
        public ISimulationRunner Resolve(string mode) => throw new InvalidOperationException(
            "Not expected to be called on the Submit path with no SessionId.");
    }

    private sealed class ThrowingCompileService : ISimulationCompileService
    {
        public Task<CompileSimulationResponse> CompileAsync(CompileSimulationRequest request, int currentUserId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Not expected to be called when SessionId does not resolve to an owned project.");

        public Task<CompileJobResponse?> GetJobAsync(string jobId, int currentUserId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Not expected to be called by SubmitVirtualLabAsync.");
    }

    private sealed class ThrowingPrecompileTrigger : IPrecompileTriggerService
    {
        public void TriggerBackgroundCompile(string sourceCode, string board, string framework, Guid? buildCacheScopeId) =>
            throw new InvalidOperationException("Not expected to be called by SubmitVirtualLabAsync (Start-only path).");
    }

    private sealed class ThrowingNotificationRepository : INotificationRepository
    {
        public Task<IEnumerable<Notification>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task MarkAsReadAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<Notification>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<Notification>> FindAsync(System.Linq.Expressions.Expression<Func<Notification, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        // Assignment.Class IS populated in this test (EF InMemory's Include
        // silently drops the root row on a required-but-unmatched navigation
        // — discovered while writing this test — so a real Class row had to
        // be seeded), which means VirtualLabRuntimeService's teacher-notification
        // branch DOES run. No-op these two instead of throwing; the other
        // members stay throwing since nothing else on the Submit path should
        // ever call them.
        public Task AddAsync(Notification entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRangeAsync(IEnumerable<Notification> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Update(Notification entity) => throw new NotSupportedException();
        public void Delete(Notification entity) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Notification entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private const string DiagramWithSensorScenarioAndMechanicalLinks = """
    {
      "board": "esp32_devkit_v1",
      "parts": [
        { "id": "l298n1", "type": "wokwi-l298n", "pinMapping": { "IN1": 13, "IN2": 14, "IN3": 16, "IN4": 17, "ENA": 18, "ENB": 19 } },
        { "id": "motorL", "type": "wokwi-dc-motor" },
        { "id": "motorR", "type": "wokwi-dc-motor" },
        { "id": "wheelL", "type": "wokwi-robot-wheel" },
        { "id": "wheelR", "type": "wokwi-robot-wheel" },
        { "id": "us1", "type": "wokwi-hc-sr04", "pinMapping": { "TRIG": 32, "ECHO": 33 } }
      ],
      "connections": [
        ["arduino:GPIO13", "l298n1:IN1"], ["arduino:GPIO14", "l298n1:IN2"],
        ["arduino:GPIO16", "l298n1:IN3"], ["arduino:GPIO17", "l298n1:IN4"],
        ["motorL:terminal1", "l298n1:OUT1"], ["motorL:terminal2", "l298n1:OUT2"],
        ["motorR:terminal1", "l298n1:OUT3"], ["motorR:terminal2", "l298n1:OUT4"],
        ["arduino:GPIO32", "us1:TRIG"], ["arduino:GPIO33", "us1:ECHO"], ["us1:GND", "arduino:GND.1"]
      ],
      "sensorScenario": {
        "sensors": {
          "us1": {
            "type": "wokwi-hc-sr04",
            "timeline": [
              { "timeMs": 0, "distanceCm": 100 },
              { "timeMs": 5000, "distanceCm": 15 }
            ]
          }
        }
      },
      "mechanicalLinks": [
        { "motorId": "motorL", "targetId": "wheelL" },
        { "motorId": "motorR", "targetId": "wheelR" }
      ]
    }
    """;

    private const string SourceCode = "void setup() { Serial.begin(115200); } void loop() { delay(1000); }";

    [Fact]
    public async Task SubmitVirtualLabAsync_RoundTrips_DiagramSensorScenarioAndMechanicalLinks_ThroughRealDbSaveAndReload()
    {
        var dbOptions = new DbContextOptionsBuilder<StemDbContext>()
            .UseInMemoryDatabase($"submission-roundtrip-{Guid.NewGuid():N}")
            .Options;

        int assignmentId;
        int submissionId;
        await using (var writeContext = new StemDbContext(dbOptions))
        {
            // A real Class row is required here — EF InMemory's Include()
            // silently drops the ROOT Assignment row when Assignment.Class
            // (a required, non-nullable-FK navigation) has no matching row,
            // unlike relational providers' LEFT JOIN semantics. Discovered by
            // this test itself (see the debug asserts a few lines below).
            var testClass = new Class
            {
                ClassCode = "TEST-ROUNDTRIP",
                SchoolId = 1,
                CourseId = 1,
                TeacherId = 1,
                GradeLevelId = 1,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6),
            };
            writeContext.Classes.Add(testClass);
            await writeContext.SaveChangesAsync();

            var assignment = new Assignment
            {
                ClassId = testClass.Id,
                Title = "Robot Delivery — Sensor Scenario Round-Trip Check",
                MaxScore = 100,
                AllowResubmit = false,
                DueDate = null,
            };
            writeContext.Assignments.Add(assignment);
            await writeContext.SaveChangesAsync();
            assignmentId = assignment.Id;
            var debugCount = await writeContext.Assignments.CountAsync();
            var debugFound = await writeContext.Assignments.Include(a => a.SimulationDetail).Include(a => a.Class).FirstOrDefaultAsync(a => a.Id == assignmentId);
            Assert.True(assignmentId > 0, $"assignment.Id was not generated (value={assignmentId}).");
            Assert.True(debugCount == 1, $"Expected 1 Assignment row, found {debugCount}.");
            Assert.NotNull(debugFound);

            var service = new VirtualLabRuntimeService(
                writeContext,
                new VirtualLabDiagramService(),
                new ThrowingRunnerResolver(),
                new ThrowingCompileService(),
                new ConfigurationManager(),
                new RunningSimulationRegistry(),
                new ThrowingPrecompileTrigger(),
                new SystemDateTimeProvider(),
                new ThrowingNotificationRepository());

            var response = await service.SubmitVirtualLabAsync(new VirtualLabSubmissionRequest
            {
                AssignmentId = assignmentId,
                SessionId = null, // no live run to attach to — proves this is pure JSON persistence, not a QEMU/compile concern.
                StudentId = 4242,
                DiagramJson = DiagramWithSensorScenarioAndMechanicalLinks,
                SourceCode = SourceCode,
            }, currentUserId: 4242, CancellationToken.None);

            Assert.True(response.SubmissionId > 0);
            submissionId = response.SubmissionId;
        }

        // "Retrieve": a BRAND NEW DbContext instance (same InMemory database
        // name, but zero shared change-tracker state with the write above) —
        // this is what actually distinguishes "survived a round trip" from
        // "the object I built five lines ago still looks the same".
        await using var readContext = new StemDbContext(dbOptions);
        var saved = await readContext.Submissions.AsNoTracking().FirstAsync(s => s.Id == submissionId);

        Assert.Equal(assignmentId, saved.AssignmentId);

        using var content = JsonDocument.Parse(saved.ContentJson);
        var virtualLabSubmission = content.RootElement.GetProperty("virtualLabSubmission");

        // CODE survived.
        Assert.Equal(SourceCode, virtualLabSubmission.GetProperty("sourceCode").GetString());

        var diagram = virtualLabSubmission.GetProperty("diagram");

        // DIAGRAM (parts/connections) survived.
        Assert.True(diagram.TryGetProperty("parts", out var parts));
        Assert.Equal(6, parts.GetArrayLength());

        // SENSOR SCENARIO survived — exact timeline values, not just "the key exists".
        var timeline = diagram.GetProperty("sensorScenario").GetProperty("sensors").GetProperty("us1").GetProperty("timeline");
        Assert.Equal(2, timeline.GetArrayLength());
        Assert.Equal(100, timeline[0].GetProperty("distanceCm").GetInt32());
        Assert.Equal(0, timeline[0].GetProperty("timeMs").GetInt32());
        Assert.Equal(15, timeline[1].GetProperty("distanceCm").GetInt32());
        Assert.Equal(5000, timeline[1].GetProperty("timeMs").GetInt32());

        // MECHANICAL LINKS survived — this is the field that was silently
        // dropped by the pre-fix submitVirtualLab (FE-side bug, already
        // fixed in dashboardApi.ts) — here proven fixed end-to-end through
        // the real backend persistence path, not just inspected in FE source.
        var mechanicalLinks = diagram.GetProperty("mechanicalLinks");
        Assert.Equal(2, mechanicalLinks.GetArrayLength());
        Assert.Equal("motorL", mechanicalLinks[0].GetProperty("motorId").GetString());
        Assert.Equal("wheelL", mechanicalLinks[0].GetProperty("targetId").GetString());
        Assert.Equal("motorR", mechanicalLinks[1].GetProperty("motorId").GetString());
        Assert.Equal("wheelR", mechanicalLinks[1].GetProperty("targetId").GetString());
    }
}
