using STEM.Application.Dtos.Schedules;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Courses;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schedules;

public class CreateScheduleHandler
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IRepository<Lesson> _lessonRepository;

    public CreateScheduleHandler(
        IScheduleRepository scheduleRepository,
        IClassRepository classRepository,
        IUserRepository userRepository,
        IAttendanceRepository attendanceRepository,
        IRepository<Lesson> lessonRepository)
    {
        _scheduleRepository = scheduleRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
        _attendanceRepository = attendanceRepository;
        _lessonRepository = lessonRepository;
    }

    public async Task<CreateScheduleResponse> Handle(CreateScheduleRequest request, int currentUserId, CancellationToken cancellationToken = default)
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

        // Validate lesson nếu được cung cấp
        Lesson? lesson = null;
        if (request.LessonId > 0)
        {
            var lessons = await _lessonRepository.FindAsync(l => l.Id == request.LessonId, cancellationToken);
            lesson = lessons.FirstOrDefault();

            if (lesson == null)
                throw new KeyNotFoundException($"Không tìm thấy bài học với id {request.LessonId}.");

            // Kiểm tra lesson đã được gán cho slot nào khác TRONG CÙNG LỚP không
            var existingSchedules = await _scheduleRepository.FindAsync(
                s => s.LessonId == request.LessonId && s.ClassId == request.ClassId, cancellationToken);

            if (existingSchedules.Any())
                throw new InvalidOperationException($"Bài học '{lesson.Title}' đã được gán cho slot khác trong lớp này.");

            // Kiểm tra lesson có thuộc course của lớp không
            // Module của lesson phải có CourseId trùng với CourseId của lớp
            if (lesson.Module != null && lesson.Module.CourseId != classEntity.CourseId)
            {
                throw new InvalidOperationException($"Bài học '{lesson.Title}' không thuộc khóa học của lớp này.");
            }
        }

        // Normalize times to UTC for comparison
        var normalizedStartTime = new DateTime(request.StartTime.Year, request.StartTime.Month, request.StartTime.Day,
                                     request.StartTime.Hour, request.StartTime.Minute, request.StartTime.Second,
                                     DateTimeKind.Utc);
        var normalizedEndTime = new DateTime(request.EndTime.Year, request.EndTime.Month, request.EndTime.Day,
                                   request.EndTime.Hour, request.EndTime.Minute, request.EndTime.Second,
                                   DateTimeKind.Utc);

        // Check for student and teacher schedule conflicts in ONE call
        var (conflicts, teacherConflicts) = await _scheduleRepository.GetAllConflictsAsync(
            request.ClassId, classEntity.TeacherId, normalizedStartTime, normalizedEndTime, cancellationToken);

        var conflictDtos = conflicts.Select(conflict => new ScheduleConflictDto
        {
            StudentId = conflict.StudentId,
            StudentName = conflict.StudentName,
            StudentEmail = conflict.StudentEmail,
            ConflictingClassId = conflict.ConflictingClassId,
            ConflictingClassCode = conflict.ConflictingClassCode,
            ConflictingClassName = string.Empty,
            ConflictingStartTime = conflict.ConflictingStartTime,
            ConflictingEndTime = conflict.ConflictingEndTime
        }).ToList();

        var teacherConflictDtos = teacherConflicts.Select(c => new TeacherConflictDto
        {
            ConflictingClassId = c.ConflictingClassId,
            ConflictingClassCode = c.ConflictingClassCode,
            ConflictingStartTime = c.ConflictingStartTime,
            ConflictingEndTime = c.ConflictingEndTime
        }).ToList();

        // Create the schedule with optional lesson
        var schedule = new Schedule
        {
            ClassId = request.ClassId,
            LessonId = request.LessonId > 0 ? request.LessonId : null,
            StartTime = normalizedStartTime,
            EndTime = normalizedEndTime,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _scheduleRepository.AddAsync(schedule, cancellationToken);
        await _scheduleRepository.SaveChangesAsync(cancellationToken);

        // Tạo attendance records cho tất cả học sinh trong lớp
        var students = classEntity.Enrollments?.Select(e => e.StudentId).ToList() ?? new List<int>();
        if (students.Any())
        {
            var attendanceRecords = students.Select(studentId => new AttendanceRecord
            {
                ClassId = schedule.ClassId,
                ScheduleId = schedule.Id,
                StudentId = studentId,
                AttendanceDate = DateOnly.FromDateTime(schedule.StartTime),
                Status = null, // Chưa điểm danh
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _attendanceRepository.AddRangeAsync(attendanceRecords, cancellationToken);
        }

        var response = new CreateScheduleResponse
        {
            Success = true,
            Schedule = new ScheduleResponse
            {
                Id = schedule.Id,
                ClassId = schedule.ClassId,
                ClassCode = classEntity.ClassCode,
                ClassName = classEntity.Course?.Title ?? string.Empty,
                LessonId = schedule.LessonId,
                LessonTitle = lesson?.Title,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                CreatedAt = schedule.CreatedAt,
                UpdatedAt = schedule.UpdatedAt
            },
            Conflicts = conflictDtos,
            TeacherConflicts = teacherConflictDtos,
            Message = BuildResponseMessage(conflictDtos.Count, teacherConflictDtos.Count)
        };

        return response;
    }

    private static string BuildResponseMessage(int studentConflictCount, int teacherConflictCount)
    {
        var messages = new List<string>();

        if (studentConflictCount > 0)
            messages.Add($"{studentConflictCount} học sinh bị trùng lịch");

        if (teacherConflictCount > 0)
            messages.Add($"giáo viên trùng lịch với lớp khác");

        if (messages.Any())
            return $"Lịch đã được tạo nhưng có: {string.Join(" và ", messages)}.";

        return "Lịch học đã được tạo thành công.";
    }
}
