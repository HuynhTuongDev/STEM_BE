using STEM.Application.Dtos.Simulation;
using STEM.Application.UseCases.Simulation;
using STEM.Application.UseCases.Simulation.Abstractions;

namespace STEM.Application.Tests;

// RUNTIME + INTERACTIVE COVERAGE BOOST milestone — Educational port of the
// digital-scripted-sensor family (PIR + the 5 generic Detected-field
// sensors + IR Obstacle), reusing the exact DHT-proven pattern: same
// SensorRuntimeHeaderGenerator.TryParseScenario source, same step-function
// timeline lookup, wired into the EXISTING generic DigitalRead/If
// instruction handling (not a new instruction kind — a real
// digitalRead(pin) call is genuinely ambiguous about what's on the pin
// until runtime, same reasoning ReadAnalog already uses for potentiometer
// vs. light sensor).
public sealed class DigitalSensorEducationalRuntimeTests
{
    private const string PirDiagram = """
    {
      "version": 1,
      "parts": [
        { "type": "board-esp32-devkit-c-v4", "id": "esp" },
        { "type": "wokwi-pir-motion-sensor", "id": "pir1" },
        { "type": "wokwi-led", "id": "led1" }
      ],
      "connections": [
        [ "pir1:OUT", "esp:GPIO14" ],
        [ "pir1:VCC", "esp:3V3" ],
        [ "pir1:GND", "esp:GND.1" ],
        [ "esp:GPIO13", "led1:A" ],
        [ "led1:C", "esp:GND.2" ]
      ]
    }
    """;

    private const string PirProgram = """
    const int PIR_PIN = 14;
    const int LED_PIN = 13;

    void setup() {
      pinMode(PIR_PIN, INPUT);
      pinMode(LED_PIN, OUTPUT);
    }

    void loop() {
      if (digitalRead(PIR_PIN) == HIGH) {
        digitalWrite(LED_PIN, HIGH);
      } else {
        digitalWrite(LED_PIN, LOW);
      }
      delay(1000);
    }
    """;

    private static string PirScenario(string componentId) => $$"""
        "sensorScenario": { "sensors": { "{{componentId}}": { "type": "wokwi-pir-motion-sensor", "timeline": [
          { "timeMs": 0, "motion": false },
          { "timeMs": 1000, "motion": true },
          { "timeMs": 2000, "motion": false }
        ] } } }
        """;

    private static string WithScenario(string diagram, string scenarioJson)
    {
        var insertAt = diagram.LastIndexOf('}');
        return diagram[..insertAt] + "," + scenarioJson + diagram[insertAt..];
    }

    [Fact]
    public async Task Pir_MotionScenario_LedTracksMotionField_ViaIfDigitalReadHigh()
    {
        var (runner, broadcaster, store, _, _) = SimulationRunnerResolverTests.CreateStreamingRunner();

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 3500,
            MaxInstructionCount = 1000,
            DiagramJson = WithScenario(PirDiagram, PirScenario("pir1")),
            SourceCode = PirProgram
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));
        await WaitForCompletionAsync(broadcaster);

        var ledEvents = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.NotEmpty(ledEvents);
        Assert.Contains(ledEvents, e => e.Time == 0 && PayloadString(e, "state") == "off");
        Assert.Contains(ledEvents, e => e.Time == 1000 && PayloadString(e, "state") == "on");
        Assert.Contains(ledEvents, e => e.Time == 2000 && PayloadString(e, "state") == "off");
    }

    [Fact]
    public async Task Pir_NoScenario_DefaultsToNoMotion_LedStaysOff()
    {
        var (runner, broadcaster, store, _, _) = SimulationRunnerResolverTests.CreateStreamingRunner();

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 1500,
            MaxInstructionCount = 1000,
            DiagramJson = PirDiagram,
            SourceCode = PirProgram
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));
        await WaitForCompletionAsync(broadcaster);

        var ledEvents = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.NotEmpty(ledEvents);
        Assert.All(ledEvents, e => Assert.Equal("off", PayloadString(e, "state")));
    }

    [Fact]
    public async Task WaterLeakSensor_GenericDetectedField_ReusesSameDigitalReadPath()
    {
        // Proves the generic (non-PIR) Detected-field branch of
        // ReadDigitalSensorScenario/DigitalSensorModel — same pattern, a
        // different physical pin name ("S" for Water Leak vs "OUT" for PIR).
        const string diagram = """
        {
          "version": 1,
          "parts": [
            { "type": "board-esp32-devkit-c-v4", "id": "esp" },
            { "type": "wokwi-water-leak-sensor", "id": "wl1" },
            { "type": "wokwi-led", "id": "led1" }
          ],
          "connections": [
            [ "wl1:S", "esp:GPIO27" ],
            [ "wl1:VCC", "esp:3V3" ],
            [ "wl1:GND", "esp:GND.1" ],
            [ "esp:GPIO13", "led1:A" ],
            [ "led1:C", "esp:GND.2" ]
          ]
        }
        """;
        const string program = """
        const int LEAK_PIN = 27;
        const int LED_PIN = 13;

        void setup() {
          pinMode(LEAK_PIN, INPUT);
          pinMode(LED_PIN, OUTPUT);
        }

        void loop() {
          if (digitalRead(LEAK_PIN) == HIGH) {
            digitalWrite(LED_PIN, HIGH);
          } else {
            digitalWrite(LED_PIN, LOW);
          }
          delay(1000);
        }
        """;
        var scenario = """
            "sensorScenario": { "sensors": { "wl1": { "type": "wokwi-water-leak-sensor", "timeline": [
              { "timeMs": 0, "detected": false },
              { "timeMs": 1000, "detected": true }
            ] } } }
            """;

        var (runner, broadcaster, store, _, _) = SimulationRunnerResolverTests.CreateStreamingRunner();

        var startResult = await runner.RunAsync(new SimulationRunContext
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            Mode = "educational",
            MaxDurationMs = 2500,
            MaxInstructionCount = 1000,
            DiagramJson = WithScenario(diagram, scenario),
            SourceCode = program
        }, CancellationToken.None);

        Assert.True(startResult.Success, string.Join("; ", startResult.Errors));
        await WaitForCompletionAsync(broadcaster);

        var ledEvents = store.AppendedEvents.Where(IsLedStateEvent).ToList();
        Assert.Contains(ledEvents, e => e.Time == 0 && PayloadString(e, "state") == "off");
        Assert.Contains(ledEvents, e => e.Time == 1000 && PayloadString(e, "state") == "on");
    }

    private static bool IsLedStateEvent(SimulationEventResponse item) =>
        item.Type == "part-state" && PayloadString(item, "component") == "led";

    private static string? PayloadString(SimulationEventResponse item, string key) =>
        item.Payload.TryGetValue(key, out var value) ? value?.ToString() : null;

    private static async Task WaitForCompletionAsync(SimulationRunnerResolverTests.FakeSimulationEventBroadcaster broadcaster)
    {
        await Task.WhenAny(broadcaster.Completed.Task, Task.Delay(TimeSpan.FromSeconds(20)));
    }
}
