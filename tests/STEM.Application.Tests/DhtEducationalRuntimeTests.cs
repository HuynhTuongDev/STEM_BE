using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;

namespace STEM.Application.Tests;

// DHT22/DHT11 Educational runtime port milestone. Same style as
// RelayModuleTests — real EducationalSimulationRunner, real diagram, real
// program, no mocking of the runner internals. Uses the EXACT documented
// contract (StemFlowDHT declaration + readTemperature()/readHumidity(),
// including the "#include \"StemFlowDHT.h\"" line from the sample exercise)
// — the Educational interpreter is regex-based, not a real compiler, so
// that line is simply an unrecognized statement it skips, unlike the QEMU
// path where it is a real (separately documented/tracked) compile bug.
public sealed class DhtEducationalRuntimeTests
{
    private const string Dht22Diagram = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-dht22", "id": "dht1" },
        { "type": "wokwi-led", "id": "led1" }
      ],
      "connections": [
        [ "dht1:SDA", "esp:GPIO19" ],
        [ "dht1:VCC", "esp:3V3" ],
        [ "dht1:GND", "esp:GND.1" ],
        [ "esp:GPIO13", "led1:A" ],
        [ "led1:C", "esp:GND.2" ]
      ]
    }
    """;

    private const string Dht11Diagram = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-dht11", "id": "dht1" },
        { "type": "wokwi-led", "id": "led1" }
      ],
      "connections": [
        [ "dht1:SDA", "esp:GPIO19" ],
        [ "dht1:VCC", "esp:3V3" ],
        [ "dht1:GND", "esp:GND.1" ],
        [ "esp:GPIO13", "led1:A" ],
        [ "led1:C", "esp:GND.2" ]
      ]
    }
    """;

    // Verbatim from virtualLabSampleExercises.ts "Bai 10" — the documented
    // contract, not a different demo API (STEP 12: "Không viết một API demo
    // khác"). 3 timeline marks is enough to prove the step-function
    // behavior without a long-running test.
    private const string DhtProgram = """
    #include "StemFlowDHT.h"

    const int LED_PIN = 13;
    const float TEMP_THRESHOLD_C = 35.0;

    StemFlowDHT dht("dht1");

    void setup() {
      pinMode(LED_PIN, OUTPUT);
    }

    void loop() {
      float temperature = dht.readTemperature();
      float humidity = dht.readHumidity();

      if (temperature > TEMP_THRESHOLD_C) {
        digitalWrite(LED_PIN, HIGH);
      } else {
        digitalWrite(LED_PIN, LOW);
      }

      delay(1000);
    }
    """;

    private static string Scenario(string componentId) => $$"""
        "sensorScenario": { "sensors": { "{{componentId}}": { "type": "wokwi-dht22", "timeline": [
          { "timeMs": 0, "temperature": 25, "humidity": 55 },
          { "timeMs": 1000, "temperature": 38, "humidity": 40 },
          { "timeMs": 2000, "temperature": 26, "humidity": 58 }
        ] } } }
        """;

    private static string WithScenario(string diagram, string componentId)
    {
        // Inject sensorScenario alongside "parts"/"connections" — same key
        // VirtualLabDiagramService/SensorRuntimeHeaderGenerator both read,
        // no new field invented (STEP 8).
        var insertAt = diagram.LastIndexOf('}');
        return diagram[..insertAt] + "," + Scenario(componentId) + diagram[insertAt..];
    }

    [Fact]
    public async Task Dht22_TemperatureCrossesThreshold_LedTurnsOnThenOff()
    {
        var (runner, broadcaster, store, _, _) = SimulationRunnerResolverTests.CreateStreamingRunner();

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 3500,
            MaxInstructionCount = 1000,
            DiagramJson = WithScenario(Dht22Diagram, "dht1"),
            SourceCode = DhtProgram
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));

        await WaitForCompletionAsync(broadcaster);

        var ledEvents = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.NotEmpty(ledEvents);
        // t=0 temp=25 (<=35) -> LED off. t=1000 temp=38 (>35) -> LED on.
        // t=2000 temp=26 (<=35) -> LED off again.
        Assert.Contains(ledEvents, e => e.Time == 0 && PayloadString(e, "state") == "off");
        Assert.Contains(ledEvents, e => e.Time == 1000 && PayloadString(e, "state") == "on");
        Assert.Contains(ledEvents, e => e.Time == 2000 && PayloadString(e, "state") == "off");
    }

    [Fact]
    public async Task Dht22_TemperatureReadAssign_EmitsCorrectScenarioValues()
    {
        var (runner, broadcaster, store, _, _) = SimulationRunnerResolverTests.CreateStreamingRunner();

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 3500,
            MaxInstructionCount = 1000,
            DiagramJson = WithScenario(Dht22Diagram, "dht1"),
            SourceCode = DhtProgram
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));
        await WaitForCompletionAsync(broadcaster);

        var tempReads = store.AppendedEvents
            .Where(e => e.Type == "pin-state" && PayloadString(e, "operation") == "dht.readTemperature")
            .ToList();
        var humReads = store.AppendedEvents
            .Where(e => e.Type == "pin-state" && PayloadString(e, "operation") == "dht.readHumidity")
            .ToList();

        Assert.Contains(tempReads, e => e.Time == 0 && PayloadDouble(e, "value") == 25.0);
        Assert.Contains(tempReads, e => e.Time == 1000 && PayloadDouble(e, "value") == 38.0);
        Assert.Contains(humReads, e => e.Time == 0 && PayloadDouble(e, "value") == 55.0);
        Assert.Contains(humReads, e => e.Time == 1000 && PayloadDouble(e, "value") == 40.0);
    }

    [Fact]
    public async Task Dht11_SamePath_TemperatureCrossesThreshold_LedReacts()
    {
        var (runner, broadcaster, store, _, _) = SimulationRunnerResolverTests.CreateStreamingRunner();

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 2500,
            MaxInstructionCount = 1000,
            DiagramJson = WithScenario(Dht11Diagram, "dht1"),
            SourceCode = DhtProgram
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));
        await WaitForCompletionAsync(broadcaster);

        var ledEvents = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.Contains(ledEvents, e => e.Time == 0 && PayloadString(e, "state") == "off");
        Assert.Contains(ledEvents, e => e.Time == 1000 && PayloadString(e, "state") == "on");
    }

    [Fact]
    public async Task NoScenario_ReadsFallBackToQemuDefaults()
    {
        // No sensorScenario at all in the diagram -> ReadDhtScenario must
        // fall back to the SAME defaults SensorRuntimeHeaderGenerator uses
        // (25.0C / 50.0%), not 0 or some other made-up value — both
        // runners must agree on "no scenario configured" behavior.
        var (runner, broadcaster, store, _, _) = SimulationRunnerResolverTests.CreateStreamingRunner();

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 1500,
            MaxInstructionCount = 1000,
            DiagramJson = Dht22Diagram,
            SourceCode = DhtProgram
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));
        await WaitForCompletionAsync(broadcaster);

        var tempReads = store.AppendedEvents
            .Where(e => e.Type == "pin-state" && PayloadString(e, "operation") == "dht.readTemperature")
            .ToList();
        Assert.NotEmpty(tempReads);
        Assert.All(tempReads, e => Assert.Equal(25.0, PayloadDouble(e, "value")));
    }

    private static bool IsLedStateEvent(SimulationEventResponse item) =>
        item.Type == "part-state" && PayloadString(item, "component") == "led";

    private static string? PayloadString(SimulationEventResponse item, string key) =>
        item.Payload.TryGetValue(key, out var value) ? value?.ToString() : null;

    private static double PayloadDouble(SimulationEventResponse item, string key)
    {
        var raw = item.Payload.TryGetValue(key, out var value) ? value : null;
        return raw switch
        {
            double d => d,
            System.Text.Json.JsonElement je => je.GetDouble(),
            _ => Convert.ToDouble(raw)
        };
    }

    private static async Task WaitForCompletionAsync(SimulationRunnerResolverTests.FakeSimulationEventBroadcaster broadcaster)
    {
        await Task.WhenAny(broadcaster.Completed.Task, Task.Delay(TimeSpan.FromSeconds(20)));
    }
}
