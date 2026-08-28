using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using STEM.Application.UseCases.Components.Abstractions;
using STEM.Application.UseCases.Components.Providers.KiCad;

namespace STEM.Infrastructure.VirtualLab.Components.Providers.KiCad;

// Second IComponentProvider implementation — proves the abstraction
// generalizes to a genuinely different data source/format (plain-text
// KiCad legacy .lib, not Fritzing's XML), not just a copy of the Fritzing
// provider with a different URL. Never called from the Run path.
public sealed class KiCadComponentProvider : IComponentProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KiCadOptions _options;
    private readonly ILogger<KiCadComponentProvider> _logger;

    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly Dictionary<string, string> _libraryContentCache = new();
    private DateTime _cachedAtUtc;

    public KiCadComponentProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<KiCadOptions> options,
        ILogger<KiCadComponentProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => KiCadConstants.ProviderName;

    public ComponentProviderCapabilities Capabilities { get; } =
        new(Metadata: true, Visual: false, Pins: true, Datasheet: false, Simulation: false);

    public async Task<IReadOnlyCollection<ExternalComponentCandidate>> SearchAsync(
        string query, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("KiCad provider disabled by configuration — SearchAsync returning empty.");
            return Array.Empty<ExternalComponentCandidate>();
        }

        var trimmedQuery = query.Trim();
        var results = new List<ExternalComponentCandidate>();

        foreach (var libraryFile in _options.LibraryFiles)
        {
            string content;
            try
            {
                content = await GetLibraryContentAsync(libraryFile, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "KiCad library fetch failed for {LibraryFile} — skipping.", libraryFile);
                continue;
            }

            var definitions = KiCadPartParser.SplitDefinitions(content)
                .Where(def => trimmedQuery.Length == 0 ||
                    def.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase));

            foreach (var (_, block) in definitions)
            {
                var candidate = KiCadPartParser.Parse(block, libraryFile, BuildHtmlUrl(libraryFile));
                if (candidate != null)
                {
                    results.Add(candidate);
                }

                if (results.Count >= _options.SearchResultLimit)
                {
                    return results;
                }
            }
        }

        return results;
    }

    public async Task<ExternalComponentCandidate?> GetAsync(string externalId, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var separatorIndex = externalId.IndexOf('#');
        if (separatorIndex <= 0)
        {
            return null;
        }

        var libraryFile = externalId[..separatorIndex];
        var symbolName = externalId[(separatorIndex + 1)..];

        string content;
        try
        {
            content = await GetLibraryContentAsync(libraryFile, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "KiCad library fetch failed for {LibraryFile} — skipping.", libraryFile);
            return null;
        }

        var match = KiCadPartParser.SplitDefinitions(content)
            .FirstOrDefault(def => def.Name.Equals(symbolName, StringComparison.Ordinal));

        return match.Block == null ? null : KiCadPartParser.Parse(match.Block, libraryFile, BuildHtmlUrl(libraryFile));
    }

    private async Task<string> GetLibraryContentAsync(string libraryFile, CancellationToken cancellationToken)
    {
        var isFresh = _libraryContentCache.ContainsKey(libraryFile) &&
            (DateTime.UtcNow - _cachedAtUtc).TotalSeconds < _options.LibraryCacheSeconds;
        if (isFresh)
        {
            return _libraryContentCache[libraryFile];
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            isFresh = _libraryContentCache.ContainsKey(libraryFile) &&
                (DateTime.UtcNow - _cachedAtUtc).TotalSeconds < _options.LibraryCacheSeconds;
            if (isFresh)
            {
                return _libraryContentCache[libraryFile];
            }

            var client = _httpClientFactory.CreateClient(KiCadConstants.ProviderName);
            var url = $"https://raw.githubusercontent.com/{_options.Owner}/{_options.Repo}/{_options.Ref}/{libraryFile}";
            var content = await client.GetStringAsync(url, cancellationToken);

            _libraryContentCache[libraryFile] = content;
            _cachedAtUtc = DateTime.UtcNow;
            return content;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private string BuildHtmlUrl(string libraryFile) =>
        $"https://github.com/{_options.Owner}/{_options.Repo}/blob/{_options.Ref}/{libraryFile}";
}
