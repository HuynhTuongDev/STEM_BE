using Microsoft.Extensions.Logging;
using STEM.Application.UseCases.Components.Abstractions;

namespace STEM.Application.UseCases.Components;

public sealed class ComponentProviderAggregator : IComponentProviderAggregator
{
    private static readonly TimeSpan DefaultPerProviderTimeout = TimeSpan.FromSeconds(10);

    private readonly IEnumerable<IComponentProvider> _providers;
    private readonly ILogger<ComponentProviderAggregator> _logger;
    private readonly TimeSpan _perProviderTimeout;

    public ComponentProviderAggregator(
        IEnumerable<IComponentProvider> providers,
        ILogger<ComponentProviderAggregator> logger,
        TimeSpan? perProviderTimeout = null)
    {
        _providers = providers;
        _logger = logger;
        _perProviderTimeout = perProviderTimeout ?? DefaultPerProviderTimeout;
    }

    public async Task<IReadOnlyCollection<ExternalComponentCandidate>> SearchAsync(
        string query, CancellationToken cancellationToken)
    {
        // Independent providers must be called concurrently (STEP 13) — a
        // sequential provider1-then-provider2 chain would make aggregated
        // search as slow as the sum of every provider's latency instead of
        // the slowest one.
        var tasks = _providers
            .Select(provider => SearchOneProviderAsync(provider, query, cancellationToken))
            .ToList();

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    // Each provider gets its own timeout and its own try/catch — one
    // provider throwing or hanging must never fail (or even slow down past
    // its own timeout) the others' results. A provider that isn't wired to
    // handle its own transient errors internally still can't take the
    // aggregator down.
    private async Task<IReadOnlyCollection<ExternalComponentCandidate>> SearchOneProviderAsync(
        IComponentProvider provider, string query, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_perProviderTimeout);

        try
        {
            return await provider.SearchAsync(query, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Component provider {Provider} timed out after {TimeoutSeconds}s — excluded from this search's results.",
                provider.ProviderName, _perProviderTimeout.TotalSeconds);
            return Array.Empty<ExternalComponentCandidate>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "Component provider {Provider} failed — excluded from this search's results.", provider.ProviderName);
            return Array.Empty<ExternalComponentCandidate>();
        }
    }
}
