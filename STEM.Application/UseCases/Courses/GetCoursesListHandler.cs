using STEM.Application.Dtos.Courses;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class GetCoursesListHandler
{
    private readonly ICourseRepository _courseRepository;

    public GetCoursesListHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<PagedCourseListResponse> Handle(
        GetCoursesRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var (courses, totalCount) = await _courseRepository.GetCoursesPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            cancellationToken);

        var items = courses.Select(c => new CourseListItemResponse
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            SyllabusId = c.SyllabusId,
            SyllabusTitle = c.Syllabus?.Title,
            EstimatedHours = c.EstimatedHours,
            IsRequired = c.IsRequired,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        return new PagedCourseListResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }
}
