using STEM.Application.UseCases.Components;

namespace STEM.Application.Tests;

public class RuntimeCapabilityResolverTests
{
    [Fact]
    public void Resolve_Led_ReturnsOutput()
    {
        var result = RuntimeCapabilityResolver.Resolve("wokwi-led");
        Assert.NotNull(result);
        Assert.Equal(RuntimeCapabilities.Output, result!.Capability);
        Assert.Null(result.SensorKind);
    }

    [Fact]
    public void Resolve_Pushbutton_ReturnsDigitalInput()
    {
        var result = RuntimeCapabilityResolver.Resolve("wokwi-pushbutton");
        Assert.NotNull(result);
        Assert.Equal(RuntimeCapabilities.DigitalInput, result!.Capability);
    }

    [Fact]
    public void Resolve_Potentiometer_ReturnsAnalogInput()
    {
        var result = RuntimeCapabilityResolver.Resolve("wokwi-potentiometer");
        Assert.NotNull(result);
        Assert.Equal(RuntimeCapabilities.AnalogInput, result!.Capability);
    }

    [Fact]
    public void Resolve_PhotoresistorSensor_ReturnsSensorInputWithLightKind()
    {
        var result = RuntimeCapabilityResolver.Resolve("wokwi-photoresistor-sensor");
        Assert.NotNull(result);
        Assert.Equal(RuntimeCapabilities.SensorInput, result!.Capability);
        Assert.Equal("light", result.SensorKind);
    }

    // STEP 15's explicit protection — a component whose SimulationComponentType
    // is null must resolve to NO capability at all, not silently inherit
    // wokwi-led's Output capability by any kind of name/category guessing.
    [Fact]
    public void Resolve_NullSimulationComponentType_ReturnsNull()
    {
        Assert.Null(RuntimeCapabilityResolver.Resolve(null));
    }

    [Fact]
    public void Resolve_UnknownSimulationComponentType_ReturnsNull()
    {
        // No runtime model exists for this type at all (display/IMU, no
        // Educational or QEMU support) — genuinely unmapped, unlike
        // wokwi-l298n/wokwi-rgb-led below which used to be here by mistake
        // (they DO have real QEMU runtime, the resolver entry was just missing).
        Assert.Null(RuntimeCapabilityResolver.Resolve("wokwi-mpu6050"));
    }

    // RUNTIME + INTERACTIVE COVERAGE BOOST milestone — L298nModel.cs and
    // RgbLedModel.cs both have real, working QEMU runtime already; the
    // resolver just never had an entry for either (a gap called out by
    // their own matrix "missingRequirements" notes).
    [Theory]
    [InlineData("wokwi-l298n")]
    [InlineData("wokwi-rgb-led")]
    public void Resolve_QemuOutputOnlyTypes_ReturnOutput(string simulationComponentType)
    {
        var result = RuntimeCapabilityResolver.Resolve(simulationComponentType);
        Assert.NotNull(result);
        Assert.Equal(RuntimeCapabilities.Output, result!.Capability);
    }

    // The 10 scripted-sensor types confirmed to already have real, ENABLED
    // QEMU support in SensorRuntimeHeaderGenerator.cs (verified by source
    // read, not assumed) — must resolve to ScriptedSensor, never SensorInput
    // (that's reserved for live/realtime-via-SignalR sensors like the
    // photoresistor).
    [Theory]
    [InlineData("wokwi-hc-sr04")]
    [InlineData("wokwi-pir-motion-sensor")]
    [InlineData("wokwi-line-tracking-sensor")]
    [InlineData("wokwi-line-tracking-3ch")]
    [InlineData("wokwi-line-tracking-5ch")]
    [InlineData("wokwi-water-leak-sensor")]
    [InlineData("wokwi-flame-sensor")]
    [InlineData("wokwi-soil-moisture-sensor")]
    [InlineData("wokwi-rain-sensor")]
    [InlineData("wokwi-vibration-sensor")]
    [InlineData("wokwi-ir-obstacle-sensor")]
    public void Resolve_ScriptedSensorTypes_ReturnScriptedSensor_NeverSensorInput(string simulationComponentType)
    {
        var result = RuntimeCapabilityResolver.Resolve(simulationComponentType);
        Assert.NotNull(result);
        Assert.Equal(RuntimeCapabilities.ScriptedSensor, result!.Capability);
        Assert.NotEqual(RuntimeCapabilities.SensorInput, result.Capability);
        Assert.NotNull(result.SensorKind);
    }

    // INTERACTIVE SENSOR CONTROLS milestone — PIR/Water Leak/Vibration are
    // the first components to genuinely need BOTH capabilities at once
    // (proven live by InteractiveDigitalSensorTests.cs), exercising
    // RuntimeCapabilityInfo.AllCapabilities for the first time.
    [Theory]
    [InlineData("wokwi-pir-motion-sensor")]
    [InlineData("wokwi-water-leak-sensor")]
    [InlineData("wokwi-vibration-sensor")]
    [InlineData("wokwi-rain-sensor")]
    public void Resolve_LiveInteractiveSensors_ReturnBothScriptedAndDigitalInput(string simulationComponentType)
    {
        var result = RuntimeCapabilityResolver.Resolve(simulationComponentType);
        Assert.NotNull(result);
        Assert.Contains(RuntimeCapabilities.ScriptedSensor, result!.AllCapabilities);
        Assert.Contains(RuntimeCapabilities.DigitalInput, result.AllCapabilities);
        Assert.Equal(2, result.AllCapabilities.Count);
    }

    [Fact]
    public void Resolve_ScriptedOnlySensor_AllCapabilities_HasExactlyOneEntry()
    {
        // HC-SR04 never got a live FE control this milestone (pulseIn/
        // microsecond arithmetic, explicitly out of scope) — AllCapabilities
        // must not silently pick up DigitalInput it was never given.
        var result = RuntimeCapabilityResolver.Resolve("wokwi-hc-sr04");
        Assert.NotNull(result);
        Assert.Single(result!.AllCapabilities);
        Assert.Equal(RuntimeCapabilities.ScriptedSensor, result.AllCapabilities[0]);
    }
}
