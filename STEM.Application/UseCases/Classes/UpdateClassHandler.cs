using STEM.Application.Dtos.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using FluentValidation;

using static STEM.Core.Entities.Users.RoleNames;

namespace STEM.Application.UseCases.Classes;

public class UpdateClassHandler
{
    private readonly IClassRepository _classRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;
    private readonly IValidator<UpdateClassRequest> _validator;

    public UpdateClassHandler(
        IClassRepository classRepository,
        ICourseRepository courseRepository,
        IUserRepository userRepository,
        IValidator<UpdateClassRequest> validator)
    {
        _classRepository = classRepository;
        _courseRepository = courseRepository;
        _userRepository = userRepository;
        _validator = validator;
    }

    public async Task<bool> Handle(
        int classId,
        UpdateClassRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        if (!RoleNames.IsSchoolAdmin(currentUser.Role?.Name) && !RoleNames.IsMasterAdmin(currentUser.Role?.Name))
            throw new UnauthorizedAccessException("Chỉ Quản trị viên trường mới được cập nhật lớp học.");

        var classEntity = await _classRepository.GetByIdAsync(classId, cancellationToken);
        if (classEntity == null)
            return false;

        if (classEntity.SchoolId != currentUser.SchoolId && !RoleNames.IsMasterAdmin(currentUser.Role?.Name))
            throw new UnauthorizedAccessException("Bạn chỉ có thể cập nhật lớp học thuộc trường của mình.");

        var course = await _courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course == null)
            throw new ArgumentException("Không tìm thấy khóa học.");

        // Course giờ không còn SchoolId, SchoolAdmin vẫn cập nhật được lớp với course
        var teacher = await _userRepository.GetByIdAsync(request.TeacherId, cancellationToken);
        if (teacher == null)
            throw new ArgumentException("Không tìm thấy giáo viên.");

        if (teacher.Role?.Name != RoleNames.Teacher)
            throw new ArgumentException("Người dùng được chỉ định không phải là giáo viên.");

        if (teacher.SchoolId != currentUser.SchoolId && !RoleNames.IsMasterAdmin(currentUser.Role?.Name))
            throw new ArgumentException("Giáo viên không thuộc trường của bạn.");

        if (classEntity.ClassCode != request.ClassCode)
        {
            var existingClass = (await _classRepository.GetClassesPagedAsync(1, 100, request.ClassCode, null, null, currentUser.SchoolId, cancellationToken)).Classes
                .FirstOrDefault(c => c.ClassCode == request.ClassCode && c.SchoolId == currentUser.SchoolId && c.Id != classId);
            if (existingClass != null)
                throw new ArgumentException("Mã lớp đã tồn tại trong trường của bạn.");
        }

        classEntity.ClassCode = request.ClassCode;
        classEntity.GradeLevelId = request.GradeLevelId;
        classEntity.CourseId = request.CourseId;
        classEntity.TeacherId = request.TeacherId;
        classEntity.StartDate = NormalizeToUtc(request.StartDate);
        classEntity.EndDate = NormalizeToUtc(request.EndDate);
        classEntity.UpdatedAt = DateTime.UtcNow;

        _classRepository.Update(classEntity);
        await _classRepository.SaveChangesAsync(cancellationToken);

        return true;
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
