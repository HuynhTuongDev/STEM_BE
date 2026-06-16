using STEM.Application.Dtos.Courses;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class CreateCourseHandler
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;

    public CreateCourseHandler(ICourseRepository courseRepository, IUserRepository userRepository)
    {
        _courseRepository = courseRepository;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(
        CreateCourseRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Enforce data isolation and permissions
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        var roleName = currentUser.Role?.Name;

        if (roleName != RoleNames.SchoolAdministrator)
            throw new UnauthorizedAccessException("Only School Administrator can create courses.");

        if (!currentUser.SchoolId.HasValue)
            throw new InvalidOperationException("School Administrator is not associated with any school.");

        // Validate teacher
        var teacher = await _userRepository.GetByIdAsync(request.TeacherId, cancellationToken);
        if (teacher == null)
            throw new ArgumentException("Teacher not found.");

        if (teacher.Role?.Name != RoleNames.Teacher)
            throw new ArgumentException("The specified user is not a teacher.");

        if (teacher.SchoolId != currentUser.SchoolId)
            throw new ArgumentException("The teacher does not belong to your school.");

        var course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            TeacherId = request.TeacherId,
            SchoolId = currentUser.SchoolId.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _courseRepository.AddAsync(course, cancellationToken);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return course.Id;
    }
}
