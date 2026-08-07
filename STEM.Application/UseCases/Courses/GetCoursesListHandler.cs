using STEM.Application.Dtos.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Courses;

public class GetCoursesListHandler
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;

    public GetCoursesListHandler(ICourseRepository courseRepository, IUserRepository userRepository)
    {
        _courseRepository = courseRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedCourseListResponse> Handle(
        GetCoursesRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Retrieve the current user to enforce data isolation
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        var roleName = currentUser.Role?.Name;

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        // Master Admin chỉ quản lý School accounts, không có quyền truy cập Course
        if (roleName == RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Master Administrator does not have access to course data.");

        // Data Isolation: Teacher/Student chỉ xem courses của trường mình
        // SchoolAdmin xem toàn bộ courses trong trường (filterSchoolId từ request hoặc trường của họ)
        int? filterSchoolId = request.SchoolId;
        if (roleName == RoleNames.Teacher || roleName == RoleNames.Student)
            filterSchoolId = currentUser.SchoolId;
        else if (roleName == RoleNames.SchoolAdministrator && filterSchoolId == null)
            filterSchoolId = currentUser.SchoolId;

        var (courses, totalCount) = await _courseRepository.GetCoursesPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            filterSchoolId,
            cancellationToken);

        var items = courses.Select(c => new CourseListItemResponse
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            SchoolId = c.SchoolId,
            SchoolName = c.School?.Name,
            CreatedAt = c.CreatedAt
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
