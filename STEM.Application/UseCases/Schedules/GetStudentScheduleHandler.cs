using STEM.Application.Dtos.Schedules;
using STEM.Core.Repository;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Schedules;

public class GetStudentScheduleHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    
    private static readonly string[] ScheduleColors = new[]
    {
        "#3b82f6", "#10b981", "#f59e0b", "#ef4444", "#8b5cf6",
        "#ec4899", "#06b6d4", "#84cc16", "#f97316", "#6366f1"
    };

    public GetStudentScheduleHandler(
        IUserRepository userRepository, 
        IScheduleRepository scheduleRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _userRepository = userRepository;
        _scheduleRepository = scheduleRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<IEnumerable<ScheduleCalendarResponse>> Handle(
        GetScheduleRequest request, 
        int currentUserId, 
        CancellationToken cancellationToken = default,
        bool isAdmin = false)
    {
        // Admin can fetch by classId
        if (isAdmin && request.ClassId.HasValue)
        {
            return await GetSchedulesByClassIdAsync(request, cancellationToken);
        }

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        // Convert dates to UTC if they have Kind=Unspecified
        DateTime? fromDateUtc = request.FromDate.HasValue 
            ? DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc) 
            : null;
        DateTime? toDateUtc = request.ToDate.HasValue 
            ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc) 
            : null;

        var schedules = await _userRepository.GetStudentSchedulesAsync(currentUserId, fromDateUtc, toDateUtc, cancellationToken);

        return schedules.Select((s, index) => new ScheduleCalendarResponse
        {
            Id = s.Id,
            ClassId = s.ClassId,
            Title = $"{s.Class?.Course?.Title ?? "Lớp học"} - {s.Class?.ClassCode}",
            Start = DateTime.SpecifyKind(s.StartTime, DateTimeKind.Utc),
            End = DateTime.SpecifyKind(s.EndTime, DateTimeKind.Utc),
            ClassCode = s.Class?.ClassCode ?? string.Empty,
            ClassName = s.Class?.Course?.Title ?? string.Empty,
            Color = ScheduleColors[index % ScheduleColors.Length],
            LessonId = s.LessonId,
            LessonTitle = s.Lesson?.Title
        });
    }

    public async Task<IEnumerable<ScheduleCalendarResponse>> HandleByClass(
        int classId,
        GetScheduleRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        // Check if user has access (enrolled student, or admin/teacher)
        if (currentUser.Role?.Name == "Student")
        {
            var enrollment = await _enrollmentRepository.FindAsync(
                e => e.StudentId == currentUserId && e.ClassId == classId, cancellationToken);
            if (!enrollment.Any())
                throw new UnauthorizedAccessException("Bạn không có quyền xem lịch của lớp học này.");
        }
        // Admin and Teacher can access any class they're teaching

        var req = new GetScheduleRequest 
        { 
            FromDate = request.FromDate, 
            ToDate = request.ToDate, 
            ClassId = classId 
        };
        return await GetSchedulesByClassIdAsync(req, cancellationToken);
    }

    private async Task<IEnumerable<ScheduleCalendarResponse>> GetSchedulesByClassIdAsync(
        GetScheduleRequest request, 
        CancellationToken cancellationToken)
    {
        DateTime? fromDateUtc = request.FromDate.HasValue 
            ? DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc) 
            : null;
        DateTime? toDateUtc = request.ToDate.HasValue 
            ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc) 
            : null;

        var schedules = await _scheduleRepository.GetByClassIdAsync(request.ClassId.Value, cancellationToken);

        // Filter by date if provided
        if (fromDateUtc.HasValue)
            schedules = schedules.Where(s => s.StartTime >= fromDateUtc.Value);
        if (toDateUtc.HasValue)
            schedules = schedules.Where(s => s.StartTime <= toDateUtc.Value);

        return schedules.Select((s, index) => new ScheduleCalendarResponse
        {
            Id = s.Id,
            ClassId = s.ClassId,
            Title = $"{s.Class?.Course?.Title ?? "Lớp học"} - {s.Class?.ClassCode}",
            Start = DateTime.SpecifyKind(s.StartTime, DateTimeKind.Utc),
            End = DateTime.SpecifyKind(s.EndTime, DateTimeKind.Utc),
            ClassCode = s.Class?.ClassCode ?? string.Empty,
            ClassName = s.Class?.Course?.Title ?? string.Empty,
            Color = ScheduleColors[index % ScheduleColors.Length],
            LessonId = s.LessonId,
            LessonTitle = s.Lesson?.Title
        });
    }
}
