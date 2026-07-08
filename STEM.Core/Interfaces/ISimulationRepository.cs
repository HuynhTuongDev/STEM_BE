using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface ISimulationRepository : IRepository<SimulationTemplate>
{
    Task<Class?> GetClassWithDetailsAsync(int classId, CancellationToken cancellationToken = default);

    Task<SimulationTemplate?> GetTemplateWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<(IEnumerable<SimulationTemplate> Templates, int TotalCount)> GetTemplatesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? classId,
        int? courseId,
        int? schoolId,
        int? teacherId,
        int? studentId,
        CancellationToken cancellationToken = default);

    Task AddSimulationAsync(SimulationEntity simulation, CancellationToken cancellationToken = default);

    void DeleteSimulation(SimulationEntity simulation);
}
