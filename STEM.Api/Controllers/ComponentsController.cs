using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STEM.Application.Dtos.Components;
using STEM.Application.UseCases.Components;
using STEM.Application.UseCases.Components.Abstractions;

namespace STEM.Api.Controllers;

// StemFlow Component API — the only component surface the Virtual Lab
// frontend is meant to call (STEP 16). External providers (Fritzing) are
// only ever reached through the /admin/* actions below, never from a
// student-facing route and never from the simulation Run path.
[ApiController]
[Route("api/components")]
[Authorize]
public class ComponentsController : ControllerBase
{
    private readonly IComponentRegistry _registry;
    private readonly IEnumerable<IComponentProvider> _providers;
    private readonly IComponentProviderAggregator _aggregator;
    private readonly IComponentNormalizer _normalizer;

    public ComponentsController(
        IComponentRegistry registry,
        IEnumerable<IComponentProvider> providers,
        IComponentProviderAggregator aggregator,
        IComponentNormalizer normalizer)
    {
        _registry = registry;
        _providers = providers;
        _aggregator = aggregator;
        _normalizer = normalizer;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var components = await _registry.ListAsync(cancellationToken);
        return Ok(components);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var component = await _registry.GetAsync(id, cancellationToken);
        return component == null ? NotFound() : Ok(component);
    }

    // provider is optional — omit it (or pass "") for an aggregated search
    // across every registered IComponentProvider (STEP 9: search endpoint
    // provider-agnostic, no /import-fritzing-style per-provider routes).
    // Passing a specific provider name still searches just that one, for
    // callers that already know where they want to look.
    [HttpGet("admin/external-components/search")]
    [Authorize(Policy = "TeacherAndAbove")]
    public async Task<IActionResult> SearchExternal(
        [FromQuery] string? provider,
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<STEM.Application.UseCases.Components.Abstractions.ExternalComponentCandidate> candidates;

        if (string.IsNullOrWhiteSpace(provider))
        {
            candidates = await _aggregator.SearchAsync(query ?? string.Empty, cancellationToken);
        }
        else
        {
            var matchedProvider = _providers.FirstOrDefault(
                item => item.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

            if (matchedProvider == null)
            {
                return BadRequest(new { message = $"Unknown component provider: {provider}." });
            }

            candidates = await matchedProvider.SearchAsync(query ?? string.Empty, cancellationToken);
        }

        // Preview computed with the exact same Normalizer + SimulationTypeResolver
        // ImportAsync would use — nothing is written to the registry here, this
        // only lets the admin see the simulation-mapping outcome (and pin count)
        // BEFORE spending an import action, without the frontend guessing at it
        // from category/name (STEP 5's explicit rule: backend decides capability).
        var response = candidates.Select(candidate =>
        {
            var normalized = _normalizer.Normalize(candidate);
            var pinIds = normalized.Pins.Select(p => p.LogicalPinId).ToList();
            return new ExternalComponentSearchResponse
            {
                Provider = candidate.Provider,
                ExternalId = candidate.ExternalId,
                Name = candidate.Name,
                Category = candidate.Category,
                SourceUrl = candidate.SourceUrl,
                License = candidate.License,
                PinCount = candidate.Pins.Count,
                SimulationTypeCandidate = SimulationTypeResolver.Resolve(normalized.Category, pinIds)
            };
        });

        return Ok(response);
    }

    [HttpPost("admin/import")]
    [Authorize(Policy = "TeacherAndAbove")]
    public async Task<IActionResult> Import(
        [FromBody] ImportComponentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var component = await _registry.ImportAsync(request.Provider, request.ExternalId, cancellationToken);
            return Ok(component);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
