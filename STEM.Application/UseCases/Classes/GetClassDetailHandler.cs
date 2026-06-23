using STEM.Application.Dtos.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Classes;

public class GetClassDetailHandler
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;

    public GetClassDetailHandler(IClassRepository classRepository, IUserRepository userRepository)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
    }

    public async Task<ClassDetailResponse> Handle(
        int classId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        var roleName = currentUser.Role?.Name;

        if (roleName != RoleNames.MasterAdministrator && roleName != RoleNames.SchoolAdministrator && roleName != RoleNames.Teacher)
            throw new UnauthorizedAccessException("Chỉ quản trị viên và giáo viên mới được xem chi tiết lớp học.");

        var classEntity = await _classRepository.GetByIdWithDetailsAsync(classId, cancellationToken);
        if (classEntity == null)
            throw new KeyNotFoundException($"Không tìm thấy lớp học với id {classId}.");

        if (classEntity.SchoolId != currentUser.SchoolId && roleName != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Bạn chỉ có thể xem lớp học thuộc trường của mình.");

        return new ClassDetailResponse
        {
            Id = classEntity.Id,
            ClassCode = classEntity.ClassCode,
            SchoolId = classEntity.SchoolId,
            SchoolName = classEntity.School?.Name,
            CourseId = classEntity.CourseId,
            CourseName = classEntity.Course?.Title ?? string.Empty,
            TeacherId = classEntity.TeacherId,
            TeacherName = classEntity.Teacher?.FullName ?? string.Empty,
            StartDate = classEntity.StartDate,
            EndDate = classEntity.EndDate,
            CreatedAt = classEntity.CreatedAt,
            UpdatedAt = classEntity.UpdatedAt,
            Students = classEntity.Enrollments?.Select(e => new StudentResponse
            {
                Id = e.StudentId,
                FullName = e.Student?.FullName ?? string.Empty,
                Email = e.Student?.Email ?? string.Empty,
                EnrolledAt = e.CreatedAt
            }).ToList() ?? new List<StudentResponse>(),
            Schedules = classEntity.Schedules?.Select(s => new ScheduleResponse
            {
                Id = s.Id,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList() ?? new List<ScheduleResponse>(),
            Announcements = classEntity.Announcements?.Select(a => new AnnouncementResponse
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                CreatedAt = a.CreatedAt
            }).ToList() ?? new List<AnnouncementResponse>()
        };
    }
}
