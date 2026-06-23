using STEM.Application.Dtos.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class GetCourseDetailHandler
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;

    public GetCourseDetailHandler(ICourseRepository courseRepository, IUserRepository userRepository)
    {
        _courseRepository = courseRepository;
        _userRepository = userRepository;
    }

    public async Task<CourseDetailResponse?> Handle(
        int courseId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        var roleName = currentUser.Role?.Name;

        if (roleName == RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Master Administrator does not have access to course data.");

        var course = await _courseRepository.GetCourseDetailAsync(courseId, cancellationToken);
        if (course == null)
            return null; // Not found

        // Data isolation logic
        if (roleName == RoleNames.SchoolAdministrator || roleName == RoleNames.Teacher || roleName == RoleNames.Student)
        {
            if (course.SchoolId != currentUser.SchoolId)
                throw new UnauthorizedAccessException("You do not have access to courses outside your school.");
        }

        return new CourseDetailResponse
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            TeacherId = course.TeacherId,
            TeacherName = course.Teacher?.FullName ?? string.Empty,
            SchoolId = course.SchoolId,
            SchoolName = course.School?.Name,
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt
        };
    }
}
