using STEM.Application.Dtos.Classes;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using FluentValidation;

using static STEM.Core.Entities.Users.RoleNames;

namespace STEM.Application.UseCases.Classes;

public class CreateClassHandler
{
    private readonly IClassRepository _classRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISchoolRepository _schoolRepository;
    private readonly IValidator<CreateClassRequest> _validator;

    public CreateClassHandler(
        IClassRepository classRepository,
        ICourseRepository courseRepository,
        IUserRepository userRepository,
        ISchoolRepository schoolRepository,
        IValidator<CreateClassRequest> validator)
    {
        _classRepository = classRepository;
        _courseRepository = courseRepository;
        _userRepository = userRepository;
        _schoolRepository = schoolRepository;
        _validator = validator;
    }

    public async Task<int> Handle(
        CreateClassRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        if (!RoleNames.IsSchoolAdmin(currentUser.Role?.Name) && !RoleNames.IsMasterAdmin(currentUser.Role?.Name))
            throw new UnauthorizedAccessException("Chỉ Quản trị viên trường mới được tạo lớp học.");

        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null)
            throw new ArgumentException("Không tìm thấy khóa học.");

        // Course giờ không còn SchoolId, SchoolAdmin vẫn tạo được lớp với course
        var teacher = await _userRepository.GetByIdAsync(request.TeacherId, cancellationToken);
        if (teacher == null)
            throw new ArgumentException("Không tìm thấy giáo viên.");

        if (teacher.Role?.Name != RoleNames.Teacher)
            throw new ArgumentException("Người dùng được chỉ định không phải là giáo viên.");

        if (teacher.SchoolId != currentUser.SchoolId && currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new ArgumentException("Giáo viên không thuộc trường của bạn.");

        if (!currentUser.SchoolId.HasValue || currentUser.SchoolId.Value == 0)
            throw new InvalidOperationException("Không xác định được trường cho lớp học.");

        var schoolId = currentUser.SchoolId.Value;

        var schoolExists = await _schoolRepository.ExistsAsync(schoolId, cancellationToken);
        if (!schoolExists)
            throw new InvalidOperationException($"Trường với ID {schoolId} không tồn tại.");

        var existingClass = (await _classRepository.GetClassesPagedAsync(1, 100, request.ClassCode, null, null, schoolId, cancellationToken)).Classes
            .FirstOrDefault(c => c.ClassCode == request.ClassCode && c.SchoolId == schoolId);
        if (existingClass != null)
            throw new ArgumentException("Mã lớp đã tồn tại trong trường của bạn.");

        var classEntity = new Class
        {
            ClassCode = request.ClassCode,
            SchoolId = schoolId,
            GradeLevelId = request.GradeLevelId,
            CourseId = request.CourseId,
            TeacherId = request.TeacherId,
            StartDate = NormalizeToUtc(request.StartDate),
            EndDate = NormalizeToUtc(request.EndDate),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _classRepository.AddAsync(classEntity, cancellationToken);
        await _classRepository.SaveChangesAsync(cancellationToken);

        return classEntity.Id;
    }

    private static DateTime NormalizeToUtc(DateTime date)
    {
        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }
}
