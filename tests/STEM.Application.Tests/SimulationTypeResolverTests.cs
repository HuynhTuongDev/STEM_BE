using STEM.Application.UseCases.Components;

namespace STEM.Application.Tests;

public class SimulationTypeResolverTests
{
    [Theory]
    [InlineData("LED", "wokwi-led")]
    [InlineData("led", "wokwi-led")]
    [InlineData("BUTTON", "wokwi-pushbutton")]
    [InlineData("BUZZER", "wokwi-buzzer")]
    [InlineData("SERVO", "wokwi-servo")]
    public void Resolve_KnownCategory_ReturnsMatchingSimulationType(string category, string expectedType)
    {
        Assert.Equal(expectedType, SimulationTypeResolver.Resolve(category));
    }

    [Theory]
    [InlineData("Some Unrecognized Sensor")]
    [InlineData(null)]
    [InlineData("")]
    public void Resolve_UnknownOrMissingCategory_ReturnsNullInsteadOfGuessing(string? category)
    {
        // "NotMapped" must be a real possible outcome, never a fuzzy/best-guess
        // fallback (STEP 8: "Không đoán simulation behavior").
        Assert.Null(SimulationTypeResolver.Resolve(category));
    }
}
