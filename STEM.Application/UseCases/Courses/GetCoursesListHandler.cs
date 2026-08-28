using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using STEM.Application.Dtos.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class GetCoursesListHandler
{
    private readonly ICourseRepository _courseRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetCoursesListHandler(
        ICourseRepository courseRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _courseRepository = courseRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedCourseListResponse> Handle(
        GetCoursesRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var isMasterAdmin = false;
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var roleName = httpContext.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? httpContext.User?.FindFirst("role")?.Value
                ?? httpContext.User?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            isMasterAdmin = string.Equals(roleName, RoleNames.MasterAdministrator, StringComparison.OrdinalIgnoreCase);
        }

        var (courses, totalCount) = await _courseRepository.GetCoursesPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            excludeArchived: !isMasterAdmin,
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
