using STEM.Application.Dtos.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class UpdateCourseHandler
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;

    public UpdateCourseHandler(ICourseRepository courseRepository, IUserRepository userRepository)
    {
        _courseRepository = courseRepository;
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(
        int courseId,
        UpdateCourseRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.SchoolAdministrator)
            throw new UnauthorizedAccessException("Only School Administrator can update courses.");

        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null)
            return false; // Not found

        if (course.SchoolId != currentUser.SchoolId)
            throw new UnauthorizedAccessException("You can only update courses from your own school.");

        // Validate teacher if it changed
        if (course.TeacherId != request.TeacherId)
        {
            var teacher = await _userRepository.GetByIdAsync(request.TeacherId, cancellationToken);
            if (teacher == null)
                throw new ArgumentException("Teacher not found.");

            if (teacher.Role?.Name != RoleNames.Teacher)
                throw new ArgumentException("The specified user is not a teacher.");

            if (teacher.SchoolId != currentUser.SchoolId)
                throw new ArgumentException("The teacher does not belong to your school.");
        }

        course.Title = request.Title;
        course.Description = request.Description;
        course.TeacherId = request.TeacherId;
        course.UpdatedAt = DateTime.UtcNow;

        _courseRepository.Update(course);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
