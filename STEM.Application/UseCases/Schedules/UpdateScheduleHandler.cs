using STEM.Application.Dtos.Schedules;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Courses;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schedules;

public class UpdateScheduleHandler
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<Lesson> _lessonRepository;

    public UpdateScheduleHandler(
        IScheduleRepository scheduleRepository,
        IClassRepository classRepository,
        IUserRepository userRepository,
        IRepository<Lesson> lessonRepository)
    {
        _scheduleRepository = scheduleRepository;
        _classRepository = classRepository;
        _userRepository = userRepository;
        _lessonRepository = lessonRepository;
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

        // Xử lý cập nhật LessonId
        Lesson? lesson = null;
        
        // Nếu có LessonId trong request, xử lý như cũ
        if (request.LessonId.HasValue)
        {
            var lessons = await _lessonRepository.FindAsync(l => l.Id == request.LessonId.Value, cancellationToken);
            lesson = lessons.FirstOrDefault();

            if (lesson == null)
                throw new KeyNotFoundException($"Không tìm thấy bài học với id {request.LessonId}.");

            // Kiểm tra lesson đã được gán cho slot khác TRONG CÙNG LỚP chưa (trừ slot hiện tại)
            var existingSchedules = await _scheduleRepository.FindAsync(
                s => s.LessonId == request.LessonId.Value && s.Id != scheduleId && s.ClassId == schedule.ClassId, cancellationToken);

            if (existingSchedules.Any())
                throw new InvalidOperationException($"Bài học '{lesson.Title}' đã được gán cho slot khác trong lớp này.");

            // Kiểm tra lesson có thuộc course của lớp không
            if (lesson.Module != null && lesson.Module.CourseId != classEntity.CourseId)
            {
                throw new InvalidOperationException($"Bài học '{lesson.Title}' không thuộc khóa học của lớp này.");
            }

            schedule.LessonId = request.LessonId;
        }
        else if (request.LessonId.HasValue == false && schedule.LessonId.HasValue)
        {
            // Cho phép xóa liên kết lesson bằng cách truyền null
            schedule.LessonId = null;
        }
        
        // Nếu schedule vẫn có LessonId (từ trước hoặc vừa được set), load lesson để trả về title
        if (schedule.LessonId.HasValue && lesson == null)
        {
            var existingLessons = await _lessonRepository.FindAsync(l => l.Id == schedule.LessonId.Value, cancellationToken);
            lesson = existingLessons.FirstOrDefault();
        }

        schedule.UpdatedAt = DateTime.UtcNow;

        await _scheduleRepository.SaveChangesAsync(cancellationToken);

        return new ScheduleResponse
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
        };
    }
}
