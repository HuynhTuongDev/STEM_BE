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

        // Chỉ MasterAdmin được phép xóa khóa học
        if (currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Only Master Administrator can delete courses.");

        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null)
            return false;

        // Check if course has any classes
        var hasClasses = await _courseRepository.HasClassesAsync(courseId, cancellationToken);
        if (hasClasses)
            throw new InvalidOperationException("Không thể xóa khóa học đã có lớp học. Vui lòng xóa các lớp học liên quan trước.");

        _courseRepository.Delete(course);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
