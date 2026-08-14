using STEM.Core.Entities.Classes;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schedules;

public class DeleteScheduleHandler
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAttendanceRepository _attendanceRepository;

    public DeleteScheduleHandler(
        IScheduleRepository scheduleRepository,
        IClassRepository classRepository,
        IUserRepository userRepository,
        IAttendanceRepository attendanceRepository)
    {
        _scheduleRepository = scheduleRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
        _attendanceRepository = attendanceRepository;
    }

    public async Task Handle(int scheduleId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        var schedule = (await _scheduleRepository.FindAsync(s => s.Id == scheduleId, cancellationToken)).FirstOrDefault();
        if (schedule == null)
            throw new KeyNotFoundException($"Không tìm thấy lịch với id {scheduleId}.");

        var classEntity = await _classRepository.GetByIdWithDetailsAsync(schedule.ClassId, cancellationToken);
        if (classEntity == null)
            throw new KeyNotFoundException("Không tìm thấy lớp học liên quan.");

        if (classEntity.SchoolId != currentUser.SchoolId)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa lịch này.");

        // Xóa attendance records trước
        await _attendanceRepository.DeleteByScheduleIdAsync(schedule.Id, cancellationToken);

        await _scheduleRepository.DeleteAsync(schedule, cancellationToken);
        await _scheduleRepository.SaveChangesAsync(cancellationToken);
    }
}
