using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IScheduleRepository : IRepository<Schedule>
{
    Task<IEnumerable<Schedule>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Schedule>> GetByRoomAndTimeAsync(int roomId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    Task<IEnumerable<Schedule>> GetByTeacherAndTimeAsync(int teacherId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    Task<Room?> GetRoomByIdAsync(int roomId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Room>> GetAllRoomsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<StudentScheduleConflict>> GetStudentScheduleConflictsAsync(int classId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeacherScheduleConflict>> GetTeacherScheduleConflictsAsync(int classId, int teacherId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    Task<(IEnumerable<StudentScheduleConflict> StudentConflicts, IEnumerable<TeacherScheduleConflict> TeacherConflicts)> GetAllConflictsAsync(int classId, int teacherId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}

public class StudentScheduleConflict
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public int ConflictingClassId { get; set; }
    public string ConflictingClassCode { get; set; } = string.Empty;
    public DateTime ConflictingStartTime { get; set; }
    public DateTime ConflictingEndTime { get; set; }
}

public class TeacherScheduleConflict
{
    public int ConflictingClassId { get; set; }
    public string ConflictingClassCode { get; set; } = string.Empty;
    public DateTime ConflictingStartTime { get; set; }
    public DateTime ConflictingEndTime { get; set; }
}