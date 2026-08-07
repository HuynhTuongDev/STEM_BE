using STEM.Application.Dtos.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Teachers;

public class GetTeachersWithClassCountHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IClassRepository _classRepository;

    public GetTeachersWithClassCountHandler(IUserRepository userRepository, IClassRepository classRepository)
    {
        _userRepository = userRepository;
        _classRepository = classRepository;
    }

    public async Task<TeachersWithClassCountResponse> Handle(TeachersWithClassCountRequest request, CancellationToken cancellationToken = default)
    {
        var (teachers, totalCount) = await _userRepository.GetTeachersWithClassCountAsync(
            request.SchoolId,
            request.Page,
            request.PageSize,
            request.Search,
            cancellationToken);

        var teacherList = teachers.ToList();

        // Get class counts for each teacher
        var classCounts = new Dictionary<int, int>();
        foreach (var teacher in teacherList)
        {
            var classes = await _classRepository.GetByTeacherIdAsync(teacher.Id, cancellationToken);
            classCounts[teacher.Id] = classes.Count();
        }

        return new TeachersWithClassCountResponse
        {
            Items = teacherList.Select(u => new TeacherWithClassCountDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Avatar = u.Avatar,
                Gender = u.Gender,
                IsActive = u.IsActive,
                IsEmailVerified = u.IsEmailVerified,
                DateOfBirth = u.DateOfBirth.HasValue ? u.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                Address = u.Address,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                SchoolId = u.SchoolId ?? 0,
                AssignedClassesCount = classCounts.TryGetValue(u.Id, out var count) ? count : 0
            }).ToList(),
            Total = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public class TeachersWithClassCountRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public int SchoolId { get; set; }
}

public class TeachersWithClassCountResponse
{
    public List<TeacherWithClassCountDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class TeacherWithClassCountDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string? Gender { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int SchoolId { get; set; }
    public int AssignedClassesCount { get; set; }
}
