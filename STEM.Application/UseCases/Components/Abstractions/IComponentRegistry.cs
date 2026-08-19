using STEM.Application.Dtos.Components;

namespace STEM.Application.UseCases.Components.Abstractions;

// StemFlow's own component registry — the only thing the Virtual Lab
// frontend/API is meant to read (STEP 16: "Virtual Lab frontend không được
// gọi trực tiếp external provider"). Import is the only place external
// providers get called; everything else here only touches local DB state.
public interface IComponentRegistry
{
    Task<ComponentDefinitionResponse> ImportAsync(
        string provider,
        string externalId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ComponentDefinitionResponse>> ListAsync(
        CancellationToken cancellationToken);

    Task<ComponentDefinitionResponse?> GetAsync(
        Guid id,
        CancellationToken cancellationToken);
}
