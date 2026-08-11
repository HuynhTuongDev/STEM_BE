using STEM.Application.Dtos.Schedules;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schedules;

public class GetTeacherScheduleHandler
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;

    private static readonly string[] ScheduleColors = new[]
    {
        "#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6",
        "#ec4899", "#06b6d4", "#84cc16", "#f97316", "#6366f1"
    };

    public GetTeacherScheduleHandler(IClassRepository classRepository, IUserRepository userRepository)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<ScheduleCalendarResponse>> Handle(GetScheduleRequest request, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        var schedules = await _classRepository.GetSchedulesByTeacherAsync(currentUserId, request.FromDate, request.ToDate, cancellationToken);

        return schedules.Select((s, index) => new ScheduleCalendarResponse
        {
            Id = s.Id,
            ClassId = s.ClassId,
            Title = $"{s.Class?.Course?.Title ?? "Lớp học"} - {s.Class?.ClassCode}",
            Start = s.StartTime,
            End = s.EndTime,
            ClassCode = s.Class?.ClassCode ?? string.Empty,
            ClassName = s.Class?.Course?.Title ?? string.Empty,
            Color = ScheduleColors[index % ScheduleColors.Length]
        });
    }
}
