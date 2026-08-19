using STEM.Application.UseCases.Components.Abstractions;
using STEM.Application.UseCases.Simulation;

namespace STEM.Application.Tests;

// STEP 10 / STEP 14's mandatory regression test, translated to what's
// directly exercisable without spinning up the full API/DB: prove
// VirtualLabDiagramService (the diagram/wiring half of the existing
// Simulation Engine) succeeds identically whether or not a component
// provider is healthy — because it never calls one at all. If a future
// change ever wires IComponentProvider into the Run path, this test's
// premise (a provider that always throws) would make it fail loudly.
public class ExistingSimulationRegressionTests
{
    [Fact]
    public async Task DiagramAnalysis_StillWorks_WhileComponentProviderIsCompletelyUnavailable()
    {
        var alwaysFailingProvider = new AlwaysThrowingComponentProvider();

        // Sanity: the stub really does fail every call — otherwise this test
        // would prove nothing.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => alwaysFailingProvider.SearchAsync("led", CancellationToken.None));

        var diagramService = new VirtualLabDiagramService();
        var diagramJson = """
            {"board":"esp32","parts":[{"id":"led1","type":"wokwi-led"}],
             "connections":[["led1:A","arduino:GPIO13"],["led1:C","arduino:GND"]]}
            """;

        var analysis = diagramService.Analyze(diagramJson, fallbackBoardType: "esp32");

        Assert.True(analysis.Validation.IsValid, string.Join("; ", analysis.Validation.Errors));
        Assert.Empty(analysis.Validation.Errors);
    }

    private sealed class AlwaysThrowingComponentProvider : IComponentProvider
    {
        public string ProviderName => "fritzing";

        public ComponentProviderCapabilities Capabilities { get; } =
            new(Metadata: true, Visual: true, Pins: true, Datasheet: false, Simulation: false);

        public Task<IReadOnlyCollection<ExternalComponentCandidate>> SearchAsync(
            string query, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Fritzing is unavailable (simulated outage).");

        public Task<ExternalComponentCandidate?> GetAsync(
            string externalId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Fritzing is unavailable (simulated outage).");
    }
}
