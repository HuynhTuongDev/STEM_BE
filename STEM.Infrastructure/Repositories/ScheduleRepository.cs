using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Classes;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class ScheduleRepository : Repository<Schedule>, IScheduleRepository
{
    public ScheduleRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Schedule>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Class)
                .ThenInclude(c => c.Course)
            .Where(s => s.ClassId == classId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> GetByRoomAndTimeAsync(int roomId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.StartTime < endTime && s.EndTime > startTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> GetByTeacherAndTimeAsync(int teacherId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.StartTime < endTime && s.EndTime > startTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Room?> GetRoomByIdAsync(int roomId, CancellationToken cancellationToken = default)
    {
        return await _context.Rooms.FindAsync(new object[] { roomId }, cancellationToken);
    }

    public async Task<IEnumerable<Room>> GetAllRoomsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rooms
            .Where(r => r.Status == "Available")
            .OrderBy(r => r.RoomCode)
            .ToListAsync(cancellationToken);
    }

    public new async Task DeleteAsync(Schedule entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
