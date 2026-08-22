using STEM.Application.Dtos.Courses;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class CreateCourseHandler
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;

    public CreateCourseHandler(
        ICourseRepository courseRepository,
        IUserRepository userRepository)
    {
        _courseRepository = courseRepository;
        _userRepository = userRepository;
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

        if (roleName != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Only Master Administrator can create courses.");

        // Check for duplicate course title globally
        var titleExists = await _courseRepository.ExistsByTitleAsync(request.Title.Trim(), cancellationToken);
        if (titleExists)
            throw new InvalidOperationException($"A course with the title '{request.Title.Trim()}' already exists.");

        var course = new Course
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            SyllabusId = request.SyllabusId,
            EstimatedHours = request.EstimatedHours,
            IsRequired = request.IsRequired,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _courseRepository.AddAsync(course, cancellationToken);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return course.Id;
    }
}
