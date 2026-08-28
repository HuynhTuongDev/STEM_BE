using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using STEM.Application.UseCases.Components.Abstractions;
using STEM.Application.UseCases.Components.Providers.Fritzing;

namespace STEM.Infrastructure.VirtualLab.Components.Providers.Fritzing;

// First (and, this phase, only) IComponentProvider implementation. Only
// called from admin search/import (ComponentsController) and never from the
// Run path — see IComponentProvider's doc comment for the invariant this
// must never violate.
public sealed class FritzingComponentProvider : IComponentProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FritzingOptions _options;
    private readonly ILogger<FritzingComponentProvider> _logger;

    // Directory listing cache (STEP 23: "External provider không được gọi
    // lại không cần thiết") — a plain field guarded by a semaphore is enough
    // for 1 provider; IMemoryCache isn't registered anywhere in this project
    // yet and adding it just for this would be exactly the kind of
    // unnecessary new dependency STEP 35 warns against.
    private readonly SemaphoreSlim _directoryLock = new(1, 1);
    private IReadOnlyList<GithubContentItem>? _cachedDirectory;
    private DateTime _cachedAtUtc;

    public FritzingComponentProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<FritzingOptions> options,
        ILogger<FritzingComponentProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => FritzingConstants.ProviderName;

    public ComponentProviderCapabilities Capabilities { get; } =
        new(Metadata: true, Visual: true, Pins: true, Datasheet: false, Simulation: false);

    public async Task<IReadOnlyCollection<ExternalComponentCandidate>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Fritzing provider disabled by configuration — SearchAsync returning empty.");
            return Array.Empty<ExternalComponentCandidate>();
        }

        IReadOnlyList<GithubContentItem> directory;
        try
        {
            directory = await GetDirectoryAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // A search that can't reach Fritzing must degrade to "no
            // results", never throw up into the aggregator/controller and
            // never touch the Run path (which never calls this at all).
            _logger.LogWarning(ex, "Fritzing directory listing failed — returning empty search results.");
            return Array.Empty<ExternalComponentCandidate>();
        }

        var trimmedQuery = query.Trim();
        var matches = directory
            .Where(item => item.Name.EndsWith(".fzp", StringComparison.OrdinalIgnoreCase))
            .Where(item => trimmedQuery.Length == 0 ||
                item.Name.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            .Take(_options.SearchResultLimit)
            .ToList();

        var results = new List<ExternalComponentCandidate>();
        foreach (var match in matches)
        {
            // Each candidate's own .fzp must be fetched to get title/family/
            // pins — a search result is only useful once parsed, matching
            // GetAsync's contract exactly (SearchAsync results are meant to
            // be directly importable without a second round-trip).
            var candidate = await TryFetchAndParseAsync(match.Path, cancellationToken);
            if (candidate != null)
            {
                results.Add(candidate);
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

        return await TryFetchAndParseAsync(externalId, cancellationToken);
    }

    private async Task<ExternalComponentCandidate?> TryFetchAndParseAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(FritzingConstants.ProviderName);
            var rawUrl = BuildRawUrl(path);
            var xml = await client.GetStringAsync(rawUrl, cancellationToken);
            var htmlUrl = $"https://github.com/{_options.Owner}/{_options.Repo}/blob/{_options.Ref}/{path}";
            return FritzingPartParser.Parse(xml, path, htmlUrl, primaryAssetUrl: null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Fritzing part fetch failed for {Path} — skipping.", path);
            return null;
        }
    }

    private async Task<IReadOnlyList<GithubContentItem>> GetDirectoryAsync(CancellationToken cancellationToken)
    {
        var isFresh = _cachedDirectory != null &&
            (DateTime.UtcNow - _cachedAtUtc).TotalSeconds < _options.DirectoryCacheSeconds;
        if (isFresh)
        {
            return _cachedDirectory!;
        }

        await _directoryLock.WaitAsync(cancellationToken);
        try
        {
            // Re-check after acquiring the lock — another caller may have
            // just refreshed it while this one was waiting.
            isFresh = _cachedDirectory != null &&
                (DateTime.UtcNow - _cachedAtUtc).TotalSeconds < _options.DirectoryCacheSeconds;
            if (isFresh)
            {
                return _cachedDirectory!;
            }

            var client = _httpClientFactory.CreateClient(FritzingConstants.ProviderName);
            var url = $"https://api.github.com/repos/{_options.Owner}/{_options.Repo}/contents/{_options.PartsFolder}?ref={_options.Ref}";
            var items = await client.GetFromJsonAsync<List<GithubContentItem>>(url, cancellationToken)
                ?? new List<GithubContentItem>();

            _cachedDirectory = items;
            _cachedAtUtc = DateTime.UtcNow;
            return items;
        }
        finally
        {
            _directoryLock.Release();
        }
    }

    private string BuildRawUrl(string path) =>
        $"https://raw.githubusercontent.com/{_options.Owner}/{_options.Repo}/{_options.Ref}/{path}";

    // Matches the real GitHub Contents API response shape, verified against
    // a live call to api.github.com/repos/fritzing/fritzing-parts/contents/core
    // (2026-08 audit) — only the fields this provider actually reads.
    public sealed class GithubContentItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;
    }
}
