using STEM.Application.Dtos.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Classes;

public class GetClassesListHandler
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;

    public GetClassesListHandler(IClassRepository classRepository, IUserRepository userRepository)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedClassListResponse> Handle(
        GetClassesRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        var roleName = currentUser.Role?.Name;

        if (roleName != RoleNames.MasterAdministrator && roleName != RoleNames.SchoolAdministrator && roleName != RoleNames.Teacher)
            throw new UnauthorizedAccessException("Chỉ quản trị viên và giáo viên mới được xem danh sách lớp học.");

        int? filterSchoolId = request.CourseId.HasValue ? null : currentUser.SchoolId;

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var (classes, totalCount) = await _classRepository.GetClassesPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            request.CourseId,
            request.TeacherId,
            filterSchoolId,
            cancellationToken
        );

        var items = classes.Select(c => new ClassListItemResponse
        {
            Id = c.Id,
            ClassCode = c.ClassCode,
            SchoolId = c.SchoolId,
            SchoolName = c.School?.Name,
            CourseId = c.CourseId,
            CourseName = c.Course?.Title ?? string.Empty,
            TeacherId = c.TeacherId,
            TeacherName = c.Teacher?.FullName ?? string.Empty,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            CreatedAt = c.CreatedAt,
            StudentCount = c.Enrollments?.Count ?? 0
        }).ToList();

        return new PagedClassListResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<List<ClassListItemResponse>> HandleTeacherClasses(
        int teacherId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.Teacher || currentUser.Id != teacherId)
            throw new UnauthorizedAccessException("Teacher can only view their own classes.");

        var classes = await _classRepository.GetByTeacherIdAsync(teacherId, cancellationToken);

        return classes
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ClassListItemResponse
            {
                Id = c.Id,
                ClassCode = c.ClassCode,
                SchoolId = c.SchoolId,
                SchoolName = c.School?.Name,
                CourseId = c.CourseId,
                CourseName = c.Course?.Title ?? string.Empty,
                TeacherId = c.TeacherId,
                TeacherName = c.Teacher?.FullName ?? string.Empty,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                CreatedAt = c.CreatedAt,
                StudentCount = c.Enrollments?.Count ?? 0
            })
            .ToList();
    }
}
