using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class DeleteCourseHandler
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;

    public DeleteCourseHandler(ICourseRepository courseRepository, IUserRepository userRepository)
    {
        _courseRepository = courseRepository;
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(
        int courseId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.SchoolAdministrator)
            throw new UnauthorizedAccessException("Only School Administrator can delete courses.");

        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null)
            return false; // Not found

        if (course.SchoolId != currentUser.SchoolId)
            throw new UnauthorizedAccessException("You can only delete courses from your own school.");

        _courseRepository.Delete(course);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
