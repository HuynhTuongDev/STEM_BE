using STEM.Application.UseCases.Simulation;

namespace STEM.Application.Tests;

// PHASE NEXT — COMPLETE LAB CATALOG FROM PROJECT DOC (2026-08-26). STEP H
// item 1 (diagram validation test) for the 5 new labs built from
// "danh sách (1).docx": Robot nhặt rác lớp học, Robot leo cầu thang nâng cao,
// Robot bóng đá mini, Robot chữa cháy tự động, Hệ thống sấy nông sản thông
// minh. These mirror the EXACT circuitConfig JSON authored in
// virtualLabSampleExercises.ts (STEM_FE) — transcribed here by hand since the
// BE test suite cannot import the FE TypeScript source directly. Real
// production VirtualLabDiagramService.Analyze() call, not a hand-rolled
// wiring check.
public sealed class PhaseNextNewLabsDiagramTests
{
    private readonly VirtualLabDiagramService _service = new();

    [Fact]
    public void TrashRobot_DiagramIsValid()
    {
        const string diagram = """
        {
          "board": "esp32_devkit_v1",
          "parts": [
            { "id": "l298n1", "type": "wokwi-l298n", "pinMapping": { "IN1": 13, "IN2": 14, "IN3": 16, "IN4": 17, "ENA": 18, "ENB": 19 } },
            { "id": "motorL", "type": "wokwi-dc-motor" },
            { "id": "motorR", "type": "wokwi-dc-motor" },
            { "id": "battery1", "type": "wokwi-battery-pack" },
            { "id": "us1", "type": "wokwi-hc-sr04", "pinMapping": { "TRIG": 32, "ECHO": 33 } },
            { "id": "servo1", "type": "wokwi-servo", "pinMapping": { "PWM": 21 } },
            { "id": "gripper1", "type": "wokwi-gripper" },
            { "id": "bin1", "type": "wokwi-sorting-box" },
            { "id": "chassis1", "type": "wokwi-robot-chassis" },
            { "id": "wheelL", "type": "wokwi-robot-wheel" },
            { "id": "wheelR", "type": "wokwi-robot-wheel" },
            { "id": "caster1", "type": "wokwi-caster-wheel" }
          ],
          "connections": [
            ["arduino:GPIO13", "l298n1:IN1"], ["arduino:GPIO14", "l298n1:IN2"],
            ["arduino:GPIO16", "l298n1:IN3"], ["arduino:GPIO17", "l298n1:IN4"],
            ["arduino:GPIO18", "l298n1:ENA"], ["arduino:GPIO19", "l298n1:ENB"],
            ["motorL:terminal1", "l298n1:OUT1"], ["motorL:terminal2", "l298n1:OUT2"],
            ["motorR:terminal1", "l298n1:OUT3"], ["motorR:terminal2", "l298n1:OUT4"],
            ["battery1:+", "l298n1:VIN"], ["battery1:-", "l298n1:GND"], ["l298n1:GND", "arduino:GND.1"],
            ["arduino:3V3", "us1:VCC"], ["arduino:GPIO32", "us1:TRIG"], ["arduino:GPIO33", "us1:ECHO"], ["us1:GND", "arduino:GND.1"],
            ["arduino:GND.2", "servo1:GND"], ["arduino:5V", "servo1:V+"], ["arduino:GPIO21", "servo1:PWM"]
          ]
        }
        """;

        var analysis = _service.Analyze(diagram, "esp32_devkit_v1");

        Assert.True(analysis.Validation.IsValid, string.Join("; ", analysis.Validation.Errors));
    }

    [Fact]
    public void StairRobot_DiagramIsValid()
    {
        const string diagram = """
        {
          "board": "esp32_devkit_v1",
          "parts": [
            { "id": "l298n1", "type": "wokwi-l298n", "pinMapping": { "IN1": 13, "IN2": 14, "IN3": 16, "IN4": 17, "ENA": 18, "ENB": 19 } },
            { "id": "motorL", "type": "wokwi-dc-motor" },
            { "id": "motorR", "type": "wokwi-dc-motor" },
            { "id": "battery1", "type": "wokwi-battery-pack" },
            { "id": "servo1", "type": "wokwi-servo", "pinMapping": { "PWM": 21 } },
            { "id": "chassis1", "type": "wokwi-robot-chassis" },
            { "id": "wheelL", "type": "wokwi-robot-wheel" },
            { "id": "wheelR", "type": "wokwi-robot-wheel" }
          ],
          "connections": [
            ["arduino:GPIO13", "l298n1:IN1"], ["arduino:GPIO14", "l298n1:IN2"],
            ["arduino:GPIO16", "l298n1:IN3"], ["arduino:GPIO17", "l298n1:IN4"],
            ["arduino:GPIO18", "l298n1:ENA"], ["arduino:GPIO19", "l298n1:ENB"],
            ["motorL:terminal1", "l298n1:OUT1"], ["motorL:terminal2", "l298n1:OUT2"],
            ["motorR:terminal1", "l298n1:OUT3"], ["motorR:terminal2", "l298n1:OUT4"],
            ["battery1:+", "l298n1:VIN"], ["battery1:-", "l298n1:GND"], ["l298n1:GND", "arduino:GND.1"],
            ["arduino:GND.2", "servo1:GND"], ["arduino:5V", "servo1:V+"], ["arduino:GPIO21", "servo1:PWM"]
          ]
        }
        """;

        var analysis = _service.Analyze(diagram, "esp32_devkit_v1");

        Assert.True(analysis.Validation.IsValid, string.Join("; ", analysis.Validation.Errors));
    }

    [Fact]
    public void SoccerRobot_DiagramIsValid()
    {
        const string diagram = """
        {
          "board": "esp32_devkit_v1",
          "parts": [
            { "id": "l298n1", "type": "wokwi-l298n", "pinMapping": { "IN1": 13, "IN2": 14, "IN3": 16, "IN4": 17, "ENA": 18, "ENB": 19 } },
            { "id": "motorL", "type": "wokwi-dc-motor" },
            { "id": "motorR", "type": "wokwi-dc-motor" },
            { "id": "battery1", "type": "wokwi-battery-pack" },
            { "id": "line1", "type": "wokwi-line-tracking-3ch", "pinMapping": { "OUT1": 21, "OUT2": 22, "OUT3": 23 } },
            { "id": "us1", "type": "wokwi-hc-sr04", "pinMapping": { "TRIG": 32, "ECHO": 33 } },
            { "id": "servo1", "type": "wokwi-servo", "pinMapping": { "PWM": 25 } },
            { "id": "ball1", "type": "wokwi-ball" },
            { "id": "chassis1", "type": "wokwi-robot-chassis" },
            { "id": "wheelL", "type": "wokwi-robot-wheel" },
            { "id": "wheelR", "type": "wokwi-robot-wheel" },
            { "id": "caster1", "type": "wokwi-caster-wheel" }
          ],
          "connections": [
            ["arduino:GPIO13", "l298n1:IN1"], ["arduino:GPIO14", "l298n1:IN2"],
            ["arduino:GPIO16", "l298n1:IN3"], ["arduino:GPIO17", "l298n1:IN4"],
            ["arduino:GPIO18", "l298n1:ENA"], ["arduino:GPIO19", "l298n1:ENB"],
            ["motorL:terminal1", "l298n1:OUT1"], ["motorL:terminal2", "l298n1:OUT2"],
            ["motorR:terminal1", "l298n1:OUT3"], ["motorR:terminal2", "l298n1:OUT4"],
            ["battery1:+", "l298n1:VIN"], ["battery1:-", "l298n1:GND"], ["l298n1:GND", "arduino:GND.1"],
            ["arduino:3V3", "line1:VCC"], ["arduino:GPIO21", "line1:OUT1"], ["arduino:GPIO22", "line1:OUT2"], ["arduino:GPIO23", "line1:OUT3"], ["line1:GND", "arduino:GND.1"],
            ["arduino:3V3", "us1:VCC"], ["arduino:GPIO32", "us1:TRIG"], ["arduino:GPIO33", "us1:ECHO"], ["us1:GND", "arduino:GND.1"],
            ["arduino:GND.2", "servo1:GND"], ["arduino:5V", "servo1:V+"], ["arduino:GPIO25", "servo1:PWM"]
          ]
        }
        """;

        var analysis = _service.Analyze(diagram, "esp32_devkit_v1");

        Assert.True(analysis.Validation.IsValid, string.Join("; ", analysis.Validation.Errors));
    }

    [Fact]
    public void FirefightRobot_DiagramIsValid()
    {
        const string diagram = """
        {
          "board": "esp32_devkit_v1",
          "parts": [
            { "id": "l298n1", "type": "wokwi-l298n", "pinMapping": { "IN1": 13, "IN2": 14, "IN3": 16, "IN4": 17, "ENA": 18, "ENB": 19 } },
            { "id": "motorL", "type": "wokwi-dc-motor" },
            { "id": "motorR", "type": "wokwi-dc-motor" },
            { "id": "battery1", "type": "wokwi-battery-pack" },
            { "id": "flame1", "type": "wokwi-flame-sensor", "pinMapping": { "DOUT": 21 } },
            { "id": "relay1", "type": "wokwi-relay-module", "pinMapping": { "IN": 25 } },
            { "id": "servo1", "type": "wokwi-servo", "pinMapping": { "PWM": 26 } },
            { "id": "tank1", "type": "wokwi-water-tank" },
            { "id": "chassis1", "type": "wokwi-robot-chassis" },
            { "id": "wheelL", "type": "wokwi-robot-wheel" },
            { "id": "wheelR", "type": "wokwi-robot-wheel" },
            { "id": "caster1", "type": "wokwi-caster-wheel" }
          ],
          "connections": [
            ["arduino:GPIO13", "l298n1:IN1"], ["arduino:GPIO14", "l298n1:IN2"],
            ["arduino:GPIO16", "l298n1:IN3"], ["arduino:GPIO17", "l298n1:IN4"],
            ["arduino:GPIO18", "l298n1:ENA"], ["arduino:GPIO19", "l298n1:ENB"],
            ["motorL:terminal1", "l298n1:OUT1"], ["motorL:terminal2", "l298n1:OUT2"],
            ["motorR:terminal1", "l298n1:OUT3"], ["motorR:terminal2", "l298n1:OUT4"],
            ["battery1:+", "l298n1:VIN"], ["battery1:-", "l298n1:GND"], ["l298n1:GND", "arduino:GND.1"],
            ["arduino:3V3", "flame1:VCC"], ["flame1:GND", "arduino:GND.1"], ["arduino:GPIO21", "flame1:DOUT"],
            ["arduino:3V3", "relay1:VCC"], ["relay1:GND", "arduino:GND.1"], ["arduino:GPIO25", "relay1:IN"],
            ["arduino:GND.2", "servo1:GND"], ["arduino:5V", "servo1:V+"], ["arduino:GPIO26", "servo1:PWM"]
          ]
        }
        """;

        var analysis = _service.Analyze(diagram, "esp32_devkit_v1");

        Assert.True(analysis.Validation.IsValid, string.Join("; ", analysis.Validation.Errors));
    }

    [Fact]
    public void DryingSystem_DiagramIsValid()
    {
        const string diagram = """
        {
          "board": "esp32_devkit_v1",
          "parts": [
            { "id": "dht1", "type": "wokwi-dht11", "pinMapping": { "SDA": 19 } },
            { "id": "fan1", "type": "wokwi-fan", "pinMapping": { "IN": 13 } },
            { "id": "relay1", "type": "wokwi-relay-module", "pinMapping": { "IN": 14 } },
            { "id": "heater1", "type": "wokwi-heating-element" },
            { "id": "battery1", "type": "wokwi-battery-pack" }
          ],
          "connections": [
            ["arduino:3V3", "dht1:VCC"], ["arduino:GPIO19", "dht1:SDA"], ["dht1:GND", "arduino:GND.1"],
            ["arduino:GPIO13", "fan1:IN"], ["fan1:+", "battery1:+"], ["fan1:-", "battery1:-"],
            ["arduino:3V3", "relay1:VCC"], ["relay1:GND", "arduino:GND.1"], ["arduino:GPIO14", "relay1:IN"],
            ["battery1:+", "relay1:COM"], ["relay1:NO", "heater1:+"], ["heater1:-", "battery1:-"]
          ]
        }
        """;

        var analysis = _service.Analyze(diagram, "esp32_devkit_v1");

        Assert.True(analysis.Validation.IsValid, string.Join("; ", analysis.Validation.Errors));
    }
}
