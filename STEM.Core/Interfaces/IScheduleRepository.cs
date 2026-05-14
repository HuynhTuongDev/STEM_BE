using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IScheduleRepository : IRepository<Schedule>
{
    Task<IEnumerable<Schedule>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default);
}