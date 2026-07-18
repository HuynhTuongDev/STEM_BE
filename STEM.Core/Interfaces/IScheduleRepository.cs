using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IScheduleRepository : IRepository<Schedule>
{
    Task<IEnumerable<Schedule>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Schedule>> GetByRoomAndTimeAsync(int roomId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    Task<IEnumerable<Schedule>> GetByTeacherAndTimeAsync(int teacherId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    Task<Room?> GetRoomByIdAsync(int roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Room>> GetAllRoomsAsync(CancellationToken cancellationToken = default);
}