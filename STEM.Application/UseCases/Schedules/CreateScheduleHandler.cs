using STEM.Application.Dtos.Schedules;
using STEM.Core.Entities.Classes;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schedules;

public class CreateScheduleHandler
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;

    public CreateScheduleHandler(
        IScheduleRepository scheduleRepository,
        IClassRepository classRepository,
        IUserRepository userRepository)
    {
        _scheduleRepository = scheduleRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
    }

    public async Task<ScheduleResponse> Handle(CreateScheduleRequest request, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        var classEntity = await _classRepository.GetByIdWithDetailsAsync(request.ClassId, cancellationToken);
        if (classEntity == null)
            throw new KeyNotFoundException($"Không tìm thấy lớp học với id {request.ClassId}.");

        if (classEntity.SchoolId != currentUser.SchoolId)
            throw new UnauthorizedAccessException("Bạn không có quyền thêm lịch cho lớp này.");

        if (request.StartTime >= request.EndTime)
            throw new InvalidOperationException("Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");

        var schedule = new Schedule
        {
            ClassId = request.ClassId,
            // Store as UTC but with same wall-clock value as local time
            // This prevents any timezone conversion during serialization
            StartTime = new DateTime(request.StartTime.Year, request.StartTime.Month, request.StartTime.Day,
                                     request.StartTime.Hour, request.StartTime.Minute, request.StartTime.Second,
                                     DateTimeKind.Utc),
            EndTime = new DateTime(request.EndTime.Year, request.EndTime.Month, request.EndTime.Day,
                                   request.EndTime.Hour, request.EndTime.Minute, request.EndTime.Second,
                                   DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _scheduleRepository.AddAsync(schedule, cancellationToken);
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
