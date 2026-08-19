using Microsoft.EntityFrameworkCore;
using STEM.Application.Dtos.Components;
using STEM.Application.UseCases.Components;
using STEM.Application.UseCases.Components.Abstractions;
using STEM.Core.Entities.Components;
using STEM.Infrastructure.Data;
using System.Text.Json;

namespace STEM.Infrastructure.VirtualLab.Components;

// Import boundary: the ONLY place in the acquisition side that writes to
// StemFlow's own Component Registry. Never called from the Run path — see
// ComponentsController for who calls this (admin import only).
public sealed class ComponentRegistryService : IComponentRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly StemDbContext _context;
    private readonly IEnumerable<IComponentProvider> _providers;
    private readonly IComponentNormalizer _normalizer;

    public ComponentRegistryService(
        StemDbContext context,
        IEnumerable<IComponentProvider> providers,
        IComponentNormalizer normalizer)
    {
        _context = context;
        _providers = providers;
        _normalizer = normalizer;
    }

    public async Task<ComponentDefinitionResponse> ImportAsync(
        string provider,
        string externalId,
        CancellationToken cancellationToken)
    {
        var matchedProvider = _providers.FirstOrDefault(
            item => item.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown component provider: {provider}.");

        var existingSource = await _context.ComponentSources
            .Include(source => source.Component)
            .ThenInclude(component => component!.Sources)
            .FirstOrDefaultAsync(
                source => source.Provider == matchedProvider.ProviderName && source.ExternalId == externalId,
                cancellationToken);

        if (existingSource?.Component != null)
        {
            // Re-importing an already-known (provider, externalId) pair is a
            // sync, not a duplicate create (STEP 15 duplicate-source test).
            existingSource.LastSyncedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return MapToResponse(existingSource.Component);
        }

        var candidate = await matchedProvider.GetAsync(externalId, cancellationToken)
            ?? throw new KeyNotFoundException($"Component {externalId} not found via provider {provider}.");

        var normalized = _normalizer.Normalize(candidate);
        var simulationType = SimulationTypeResolver.Resolve(normalized.Category);

        // Transitional canonical key policy (STEP 3): the normalizer's
        // generated candidate is used as-is. Real cross-provider dedup
        // (STEP 15, "Arduino Uno"/"Arduino UNO R3" -> one canonical key) is
        // explicitly out of scope until a 2nd provider exists to prove it
        // against — see PARTS THAT NEED EXTENSION note in the baseline
        // report; ComponentAggregator is where that logic belongs.
        var canonicalKey = normalized.CanonicalKeyCandidate;

        var component = await _context.ComponentDefinitions
            .Include(c => c.Sources)
            .FirstOrDefaultAsync(c => c.CanonicalKey == canonicalKey, cancellationToken);

        var pinsJson = JsonSerializer.Serialize(
            normalized.Pins.Select(pin => new
            {
                visualPinId = pin.VisualPinId,
                logicalPinId = pin.LogicalPinId,
                simulationPinId = simulationType != null ? pin.LogicalPinId : null,
                aliases = pin.Aliases
            }),
            JsonOptions);

        if (component == null)
        {
            component = new ComponentDefinition
            {
                Id = Guid.NewGuid(),
                CanonicalKey = canonicalKey,
                Name = normalized.Name,
                Category = normalized.Category,
                Status = ComponentDefinitionStatuses.Normalized,
                SimulationComponentType = simulationType,
                PinsJson = pinsJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            AdvanceStatus(component, normalized, simulationType);
            _context.ComponentDefinitions.Add(component);
        }
        else
        {
            component.PinsJson = pinsJson;
            component.SimulationComponentType ??= simulationType;
            component.UpdatedAt = DateTime.UtcNow;
            AdvanceStatus(component, normalized, component.SimulationComponentType);
        }

        var source = new ComponentSource
        {
            Id = Guid.NewGuid(),
            ComponentId = component.Id,
            Component = component,
            Provider = matchedProvider.ProviderName,
            ExternalId = candidate.ExternalId,
            SourceUrl = candidate.SourceUrl,
            License = candidate.License,
            LicenseStatus = candidate.License == null
                ? ComponentLicenseStatuses.Unknown
                : ComponentLicenseStatuses.ReviewRequired,
            ExternalVersion = candidate.ExternalVersion,
            Checksum = candidate.Checksum,
            AssetsJson = candidate.PrimaryAssetUrl == null
                ? "[]"
                : JsonSerializer.Serialize(new[] { candidate.PrimaryAssetUrl }, JsonOptions),
            ImportedAt = DateTime.UtcNow
        };
        // Only add via the DbSet — setting source.Component above already
        // makes EF Core's relationship fixup add this to
        // component.Sources too; also calling component.Sources.Add(source)
        // here double-added it to the in-memory collection (found via a
        // real import call: the response listed the same source twice).
        _context.ComponentSources.Add(source);

        await _context.SaveChangesAsync(cancellationToken);
        return MapToResponse(component);
    }

    public async Task<IReadOnlyCollection<ComponentDefinitionResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var components = await _context.ComponentDefinitions
            .AsNoTracking()
            .Include(c => c.Sources)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return components.Select(MapToResponse).ToList();
    }

    public async Task<ComponentDefinitionResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var component = await _context.ComponentDefinitions
            .AsNoTracking()
            .Include(c => c.Sources)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return component == null ? null : MapToResponse(component);
    }

    private static void AdvanceStatus(ComponentDefinition component, NormalizedComponent normalized, string? simulationType)
    {
        if (component.Status == ComponentDefinitionStatuses.Verified)
        {
            return; // never regress a human-verified component automatically.
        }

        component.Status = normalized.Pins.Count == 0
            ? ComponentDefinitionStatuses.Normalized
            : simulationType != null
                ? ComponentDefinitionStatuses.SimulationMapped
                : ComponentDefinitionStatuses.PinMapped;
    }

    private static ComponentDefinitionResponse MapToResponse(ComponentDefinition component)
    {
        var pins = JsonSerializer.Deserialize<List<PinRecord>>(component.PinsJson, JsonOptions) ?? new();

        return new ComponentDefinitionResponse
        {
            Id = component.Id,
            CanonicalKey = component.CanonicalKey,
            Name = component.Name,
            Category = component.Category,
            Status = component.Status,
            SimulationComponentType = component.SimulationComponentType,
            Pins = pins.Select(pin => new ComponentPinResponse
            {
                VisualPinId = pin.VisualPinId,
                LogicalPinId = pin.LogicalPinId,
                Aliases = pin.Aliases ?? Array.Empty<string>()
            }).ToList(),
            Sources = component.Sources.Select(source => new ComponentSourceResponse
            {
                Provider = source.Provider,
                ExternalId = source.ExternalId,
                SourceUrl = source.SourceUrl,
                License = source.License,
                LicenseStatus = source.LicenseStatus,
                ExternalVersion = source.ExternalVersion,
                ImportedAt = source.ImportedAt
            }).ToList()
        };
    }

    private sealed class PinRecord
    {
        public string VisualPinId { get; set; } = string.Empty;
        public string LogicalPinId { get; set; } = string.Empty;
        public string[]? Aliases { get; set; }
    }
}
