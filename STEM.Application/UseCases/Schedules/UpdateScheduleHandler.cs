using STEM.Application.Dtos.Schedules;
using STEM.Core.Entities.Classes;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schedules;

public class UpdateScheduleHandler
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;

    public UpdateScheduleHandler(
        IScheduleRepository scheduleRepository,
        IClassRepository classRepository,
        IUserRepository userRepository)
    {
        _scheduleRepository = scheduleRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
    }

    public async Task<ScheduleResponse> Handle(int scheduleId, UpdateScheduleRequest request, int currentUserId, CancellationToken cancellationToken = default)
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
            throw new UnauthorizedAccessException("Bạn không có quyền cập nhật lịch này.");

        var newStartTime = request.StartTime ?? schedule.StartTime;
        var newEndTime = request.EndTime ?? schedule.EndTime;

        if (newStartTime >= newEndTime)
            throw new InvalidOperationException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

        if (request.StartTime.HasValue)
            schedule.StartTime = DateTime.SpecifyKind(request.StartTime.Value, DateTimeKind.Utc);
        if (request.EndTime.HasValue)
            schedule.EndTime = DateTime.SpecifyKind(request.EndTime.Value, DateTimeKind.Utc);

        schedule.UpdatedAt = DateTime.UtcNow;

        await _scheduleRepository.SaveChangesAsync(cancellationToken);

        return new ScheduleResponse
        {
            Id = schedule.Id,
            ClassId = schedule.ClassId,
            ClassCode = classEntity.ClassCode,
            ClassName = classEntity.Course?.Title ?? string.Empty,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt
        };
    }
}
