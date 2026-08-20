using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Components;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.Tests;

// Relay Module milestone (Class D -> B): mirrors the existing LED/Potentiometer/
// LightSensor test style — real EducationalSimulationRunner, real diagram,
// real program, no mocking of the runner internals.
public sealed class RelayModuleTests
{
    private const string ValidRelayDiagram = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-relay-module", "id": "relay1" }
      ],
      "connections": [
        [ "relay1:IN", "esp:GPIO13" ],
        [ "relay1:VCC", "esp:5V" ],
        [ "relay1:GND", "esp:GND.1" ]
      ]
    }
    """;

    private const string RelayBlinkProgram = """
    const int RELAY = 13;

    void setup() {
      pinMode(RELAY, OUTPUT);
    }

    void loop() {
      digitalWrite(RELAY, HIGH);
      delay(1000);

      digitalWrite(RELAY, LOW);
      delay(1000);
    }
    """;

    // === Capability (STEP 9/13) ===

    [Fact]
    public void RuntimeCapabilityResolver_Relay_ReturnsOutput_NotDigitalInput()
    {
        var result = RuntimeCapabilityResolver.Resolve("wokwi-relay-module");
        Assert.NotNull(result);
        Assert.Equal(RuntimeCapabilities.Output, result!.Capability);
        Assert.Null(result.SensorKind);
    }

    // === Wiring validation (STEP 5/13) ===

    [Fact]
    public void Analyze_ValidRelayWiring_IsValid()
    {
        var service = new VirtualLabDiagramService();
        var result = service.Analyze(ValidRelayDiagram);
        Assert.True(result.Validation.IsValid, string.Join("; ", result.Validation.Errors));
    }

    [Fact]
    public void Analyze_RelayInNotConnectedToGpio_IsInvalid()
    {
        var service = new VirtualLabDiagramService();
        var diagram = """
        {
          "version": 1,
          "parts": [
            { "type": "board-esp32-devkit-c-v4", "id": "esp" },
            { "type": "wokwi-relay-module", "id": "relay1" }
          ],
          "connections": [
            [ "relay1:VCC", "esp:5V" ],
            [ "relay1:GND", "esp:GND.1" ]
          ]
        }
        """;

        var result = service.Analyze(diagram);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("Relay IN must reach an ESP32 GPIO"));
    }

    [Fact]
    public void Analyze_RelayMissingGround_IsInvalid()
    {
        var service = new VirtualLabDiagramService();
        var diagram = """
        {
          "version": 1,
          "parts": [
            { "type": "board-esp32-devkit-c-v4", "id": "esp" },
            { "type": "wokwi-relay-module", "id": "relay1" }
          ],
          "connections": [
            [ "relay1:IN", "esp:GPIO13" ],
            [ "relay1:VCC", "esp:5V" ]
          ]
        }
        """;

        var result = service.Analyze(diagram);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Contains("Relay must connect to GND"));
    }

    // === Educational runtime (STEP 6/10) ===

    [Fact]
    public async Task RelayBlinkProgram_ReactsLive_HighTurnsOn_LowTurnsOff_SingleRun()
    {
        var (runner, broadcaster, store, _, _) = SimulationRunnerResolverTests.CreateStreamingRunner();

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 3500,
            MaxInstructionCount = 1000,
            DiagramJson = ValidRelayDiagram,
            SourceCode = RelayBlinkProgram
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));

        await WaitForCompletionAsync(broadcaster);

        var relayEvents = store.AppendedEvents.Where(IsRelayStateEvent).ToList();
        Assert.NotEmpty(relayEvents);
        Assert.Contains(relayEvents, item => item.Time == 0 && PayloadString(item, "state") == "on");
        Assert.Contains(relayEvents, item => item.Time == 1000 && PayloadString(item, "state") == "off");
        Assert.Equal(VirtualLabProjectStatuses.Running, store.FinalStatus);
    }

    private static bool IsRelayStateEvent(SimulationEventResponse item)
    {
        return item.Type == "part-state" && PayloadString(item, "component") == "relay";
    }

    private static string? PayloadString(SimulationEventResponse item, string key)
    {
        return item.Payload.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private static async Task WaitForCompletionAsync(SimulationRunnerResolverTests.FakeSimulationEventBroadcaster broadcaster)
    {
        await Task.WhenAny(broadcaster.Completed.Task, Task.Delay(TimeSpan.FromSeconds(20)));
    }
}
