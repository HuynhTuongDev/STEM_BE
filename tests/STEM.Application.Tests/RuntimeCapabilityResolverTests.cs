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
    // is null (e.g. every RGB LED variant, per the earlier dedup/mapping bug
    // fixes) must resolve to NO capability at all, not silently inherit
    // wokwi-led's Output capability by any kind of name/category guessing.
    [Fact]
    public void Resolve_NullSimulationComponentType_ReturnsNull()
    {
        Assert.Null(RuntimeCapabilityResolver.Resolve(null));
    }

    [Fact]
    public void Resolve_UnknownSimulationComponentType_ReturnsNull()
    {
        Assert.Null(RuntimeCapabilityResolver.Resolve("wokwi-rgb-led"));
    }
}
