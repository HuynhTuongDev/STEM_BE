using Microsoft.Extensions.Logging;
using STEM.Application.Dtos.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class UpdateCourseHandler
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateCourseHandler> _logger;

    public UpdateCourseHandler(
        ICourseRepository courseRepository,
        IUserRepository userRepository,
        ILogger<UpdateCourseHandler> logger)
    {
        _courseRepository = courseRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        int courseId,
        UpdateCourseRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UpdateCourse called - CourseId: {CourseId}, Title: {Title}", courseId, request.Title);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.SchoolAdministrator)
            throw new UnauthorizedAccessException("Only School Administrator can update courses.");

        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null)
            return false;

        if (course.SchoolId != currentUser.SchoolId)
            throw new UnauthorizedAccessException("You can only update courses from your own school.");

        if (!string.IsNullOrWhiteSpace(request.Title))
            course.Title = request.Title.Trim();
        if (request.Description != null)
            course.Description = request.Description.Trim();
        course.UpdatedAt = DateTime.UtcNow;

        _courseRepository.Update(course);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
