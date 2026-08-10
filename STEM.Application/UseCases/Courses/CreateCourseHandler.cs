using STEM.Application.Dtos.Courses;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class CreateCourseHandler
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISchoolRepository _schoolRepository;

    public CreateCourseHandler(
        ICourseRepository courseRepository,
        IUserRepository userRepository,
        ISchoolRepository schoolRepository)
    {
        _courseRepository = courseRepository;
        _userRepository = userRepository;
        _schoolRepository = schoolRepository;
    }

    public async Task<int> Handle(
        CreateCourseRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        var roleName = currentUser.Role?.Name;

        if (roleName != RoleNames.SchoolAdministrator)
            throw new UnauthorizedAccessException("Only School Administrator can create courses.");

        if (!currentUser.SchoolId.HasValue)
            throw new InvalidOperationException("School Administrator is not associated with any school.");

        var schoolExists = await _schoolRepository.ExistsAsync(currentUser.SchoolId.Value, cancellationToken);
        if (!schoolExists)
            throw new InvalidOperationException($"School with ID {currentUser.SchoolId.Value} does not exist. Please contact your administrator.");

        // Check for duplicate course title in the same school
        var titleExists = await _courseRepository.ExistsByTitleAsync(request.Title.Trim(), currentUser.SchoolId.Value, cancellationToken);
        if (titleExists)
            throw new InvalidOperationException($"A course with the title '{request.Title.Trim()}' already exists in your school.");

        var course = new Course
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            SchoolId = currentUser.SchoolId.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _courseRepository.AddAsync(course, cancellationToken);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return course.Id;
    }
}
