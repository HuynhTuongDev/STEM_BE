using STEM.Application.Dtos.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class GetCourseDetailHandler
{
    private readonly ICourseRepository _courseRepository;

    public GetCourseDetailHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<CourseDetailResponse?> Handle(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        var course = await _courseRepository.GetCourseDetailAsync(courseId, cancellationToken);
        if (course == null)
            return null;

        return new CourseDetailResponse
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            SyllabusId = course.SyllabusId,
            SyllabusTitle = course.Syllabus?.Title,
            EstimatedHours = course.EstimatedHours,
            IsRequired = course.IsRequired,
            IsActive = course.IsActive,
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt
        };
    }
}
