namespace STEM.Application.UseCases.Components.Abstractions;

// Fans a search out to every registered IComponentProvider concurrently.
// One provider failing (throw/timeout) must never fail the whole search —
// see ComponentProviderAggregator's implementation notes. Candidates already
// carry their own Provider field, so callers can tell results apart without
// a separate wrapper type.
public interface IComponentProviderAggregator
{
    Task<IReadOnlyCollection<ExternalComponentCandidate>> SearchAsync(
        string query,
        CancellationToken cancellationToken);
}
