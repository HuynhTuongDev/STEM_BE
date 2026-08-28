using Microsoft.Extensions.Logging.Abstractions;
using STEM.Application.UseCases.Components;
using STEM.Application.UseCases.Components.Abstractions;

namespace STEM.Application.Tests;

public class ComponentProviderAggregatorTests
{
    [Fact]
    public async Task SearchAsync_BothProvidersSucceed_ReturnsCombinedResults()
    {
        var providerA = new StubProvider("a", StubBehavior.Success, MakeCandidate("a", "1"));
        var providerB = new StubProvider("b", StubBehavior.Success, MakeCandidate("b", "1"));
        var aggregator = new ComponentProviderAggregator(new[] { providerA, providerB }, NullLogger<ComponentProviderAggregator>.Instance);

        var results = await aggregator.SearchAsync("led", CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Provider == "a");
        Assert.Contains(results, r => r.Provider == "b");
    }

    [Fact]
    public async Task SearchAsync_ProviderAThrows_StillReturnsProviderBResults()
    {
        var providerA = new StubProvider("a", StubBehavior.Throw);
        var providerB = new StubProvider("b", StubBehavior.Success, MakeCandidate("b", "1"));
        var aggregator = new ComponentProviderAggregator(new[] { providerA, providerB }, NullLogger<ComponentProviderAggregator>.Instance);

        var results = await aggregator.SearchAsync("led", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("b", results.Single().Provider);
    }

    [Fact]
    public async Task SearchAsync_ProviderBThrows_StillReturnsProviderAResults()
    {
        var providerA = new StubProvider("a", StubBehavior.Success, MakeCandidate("a", "1"));
        var providerB = new StubProvider("b", StubBehavior.Throw);
        var aggregator = new ComponentProviderAggregator(new[] { providerA, providerB }, NullLogger<ComponentProviderAggregator>.Instance);

        var results = await aggregator.SearchAsync("led", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("a", results.Single().Provider);
    }

    [Fact]
    public async Task SearchAsync_AllProvidersFail_ReturnsEmptyNotThrow()
    {
        var providerA = new StubProvider("a", StubBehavior.Throw);
        var providerB = new StubProvider("b", StubBehavior.Throw);
        var aggregator = new ComponentProviderAggregator(new[] { providerA, providerB }, NullLogger<ComponentProviderAggregator>.Instance);

        var results = await aggregator.SearchAsync("led", CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ProviderTimesOut_ExcludedWithoutFailingOthers()
    {
        var providerA = new StubProvider("a", StubBehavior.Hang);
        var providerB = new StubProvider("b", StubBehavior.Success, MakeCandidate("b", "1"));
        // Short timeout override — proves the aggregator's own per-provider
        // timeout fires (not just try/catch around an immediate throw)
        // without making this test wait out the real 10s production default.
        var aggregator = new ComponentProviderAggregator(
            new[] { providerA, providerB },
            NullLogger<ComponentProviderAggregator>.Instance,
            perProviderTimeout: TimeSpan.FromMilliseconds(200));

        var results = await aggregator.SearchAsync("led", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("b", results.Single().Provider);
    }

    private static ExternalComponentCandidate MakeCandidate(string provider, string externalId) => new(
        Provider: provider,
        ExternalId: externalId,
        Name: "Test Component",
        Category: "LED",
        SourceUrl: "https://example.com",
        License: null,
        ExternalVersion: null,
        Checksum: null,
        Pins: Array.Empty<ExternalPinCandidate>(),
        PrimaryAssetUrl: null);

    private enum StubBehavior { Success, Throw, Hang }

    private sealed class StubProvider : IComponentProvider
    {
        private readonly StubBehavior _behavior;
        private readonly ExternalComponentCandidate? _result;

        public StubProvider(string name, StubBehavior behavior, ExternalComponentCandidate? result = null)
        {
            ProviderName = name;
            _behavior = behavior;
            _result = result;
        }

        public string ProviderName { get; }

        public ComponentProviderCapabilities Capabilities { get; } =
            new(Metadata: true, Visual: false, Pins: true, Datasheet: false, Simulation: false);

        public async Task<IReadOnlyCollection<ExternalComponentCandidate>> SearchAsync(
            string query, CancellationToken cancellationToken)
        {
            switch (_behavior)
            {
                case StubBehavior.Throw:
                    throw new InvalidOperationException($"{ProviderName} is unavailable (simulated).");
                case StubBehavior.Hang:
                    // Never completes on its own — proves the aggregator's
                    // own per-provider timeout (not just try/catch) kicks in.
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                    return Array.Empty<ExternalComponentCandidate>();
                default:
                    return _result == null ? Array.Empty<ExternalComponentCandidate>() : new[] { _result };
            }
        }

        public Task<ExternalComponentCandidate?> GetAsync(string externalId, CancellationToken cancellationToken) =>
            Task.FromResult(_result);
    }
}
