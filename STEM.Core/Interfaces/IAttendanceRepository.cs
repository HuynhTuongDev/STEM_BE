using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IAttendanceRepository : IRepository<Attendee>
{
    Task<IEnumerable<Attendee>> GetByScheduleIdAsync(int scheduleId, CancellationToken cancellationToken = default);
}