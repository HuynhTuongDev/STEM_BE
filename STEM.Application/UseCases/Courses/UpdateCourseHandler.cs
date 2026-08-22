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

        if (currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Only Master Administrator can update courses.");

        var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken);
        if (course == null)
            return false;

        // Check for duplicate course title
        if (!string.IsNullOrWhiteSpace(request.Title) && course.Title.ToLower() != request.Title.Trim().ToLower())
        {
            var titleExists = await _courseRepository.ExistsByTitleAsync(request.Title.Trim(), cancellationToken);
            if (titleExists)
                throw new InvalidOperationException($"A course with the title '{request.Title.Trim()}' already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
            course.Title = request.Title.Trim();
        if (request.Description != null)
            course.Description = request.Description.Trim();
        course.SyllabusId = request.SyllabusId;
        course.EstimatedHours = request.EstimatedHours;
        course.IsRequired = request.IsRequired;
        course.IsActive = request.IsActive;
        course.UpdatedAt = DateTime.UtcNow;

        _courseRepository.Update(course);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
