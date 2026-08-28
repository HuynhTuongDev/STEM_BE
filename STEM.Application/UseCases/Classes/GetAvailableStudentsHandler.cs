using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Application.Dtos.Students;
using System.Linq;

namespace STEM.Application.UseCases.Classes;

public class GetAvailableStudentsHandler
{
    private const string StudentRoleName = "Student";

    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IRepository<Role> _roleRepository;

    public GetAvailableStudentsHandler(
        IClassRepository classRepository,
        IUserRepository userRepository,
        IEnrollmentRepository enrollmentRepository,
        IRepository<Role> roleRepository)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
        _enrollmentRepository = enrollmentRepository;
        _roleRepository = roleRepository;
    }

    public async Task<AvailableStudentsResponse> Handle(int classId, int currentUserId, AvailableStudentsRequest request)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        var classEntity = await _classRepository.GetByIdAsync(classId);
        if (classEntity == null)
            throw new KeyNotFoundException("Không tìm thấy lớp học.");

        if (classEntity.SchoolId != currentUser.SchoolId && currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Bạn không có quyền xem lớp học này.");

        // Get schedules of this class
        var classSchedules = await _classRepository.GetSchedulesAsync(classId);
        if (!classSchedules.Any())
        {
            // No schedule = all students are available
            return await GetAllAvailableStudentsAsync(classId, currentUser.SchoolId ?? 0, request);
        }

        // Get student role
        var studentRole = (await _roleRepository.FindAsync(r => r.Name == StudentRoleName)).FirstOrDefault();
        if (studentRole == null)
            return new AvailableStudentsResponse { Students = new List<StudentResponse>(), Page = 1, PageSize = request.PageSize };

        // Get students already enrolled in this class
        var enrolledStudentIds = (await _enrollmentRepository.GetByClassIdAsync(classId))
            .Select(e => e.StudentId)
            .ToHashSet();

        // Get all students in the school
        var allStudents = (await _userRepository.FindAsync(u =>
            u.SchoolId == currentUser.SchoolId &&
            u.RoleId == studentRole.Id)).ToList();

        var availableStudents = new List<StudentResponse>();
        var unavailableCount = 0;

        foreach (var student in allStudents)
        {
            // Skip if already enrolled
            if (enrolledStudentIds.Contains(student.Id))
                continue;
            
            // Skip inactive/deleted students
            if (!student.IsActive)
                continue;

            // Check if student is already enrolled in another class with the same course
            var existingCourseEnrollment = await _enrollmentRepository.GetExistingCourseEnrollmentAsync(
                student.Id, classEntity.CourseId, classId);
            if (existingCourseEnrollment != null)
            {
                unavailableCount++;
                continue;
            }

            // Check if student has any conflicting schedule
            var canAdd = await _enrollmentRepository.CanAddStudentToClassAsync(student.Id, classId);
            if (canAdd)
            {
                availableStudents.Add(new StudentResponse
                {
                    Id = student.Id,
                    FullName = student.FullName,
                    Email = student.Email,
                    Phone = student.Phone,
                    Avatar = student.Avatar,
                    Gender = student.Gender,
                    IsActive = student.IsActive,
                    CreatedAt = student.CreatedAt
                });
            }
            else
            {
                unavailableCount++;
            }
        }

        // Apply pagination
        var totalCount = availableStudents.Count;
        var pagedStudents = availableStudents
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new AvailableStudentsResponse
        {
            Students = pagedStudents,
            TotalCount = totalCount,
            UnavailableCount = unavailableCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    private async Task<AvailableStudentsResponse> GetAllAvailableStudentsAsync(int classId, int schoolId, AvailableStudentsRequest request)
    {
        var classEntity = await _classRepository.GetByIdAsync(classId);

        // Get student role
        var studentRole = (await _roleRepository.FindAsync(r => r.Name == StudentRoleName)).FirstOrDefault();
        if (studentRole == null)
            return new AvailableStudentsResponse { Students = new List<StudentResponse>(), Page = 1, PageSize = request.PageSize };

        // Get students already enrolled in this class
        var enrolledStudentIds = (await _enrollmentRepository.GetByClassIdAsync(classId))
            .Select(e => e.StudentId)
            .ToHashSet();

        // Get all students in the school (excluding already enrolled and inactive)
        var allStudents = (await _userRepository.FindAsync(u =>
            u.SchoolId == schoolId &&
            u.RoleId == studentRole.Id &&
            u.IsActive))
            .Where(s => !enrolledStudentIds.Contains(s.Id)).ToList();

        // Filter out students already enrolled in another class with the same course
        if (classEntity != null)
        {
            var studentsWithCourseConflict = new HashSet<int>();
            foreach (var student in allStudents)
            {
                var existingCourseEnrollment = await _enrollmentRepository.GetExistingCourseEnrollmentAsync(
                    student.Id, classEntity.CourseId, classId);
                if (existingCourseEnrollment != null)
                {
                    studentsWithCourseConflict.Add(student.Id);
                }
            }
            allStudents = allStudents.Where(s => !studentsWithCourseConflict.Contains(s.Id)).ToList();
        }

        var totalCount = allStudents.Count;
        var pagedStudents = allStudents
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StudentResponse
            {
                Id = s.Id,
                FullName = s.FullName,
                Email = s.Email,
                Phone = s.Phone,
                Avatar = s.Avatar,
                Gender = s.Gender,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToList();

        return new AvailableStudentsResponse
        {
            Students = pagedStudents,
            TotalCount = totalCount,
            UnavailableCount = 0,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    public async Task<List<AvailableTeacherResponse>> HandleGetAvailableTeachers(int classId, CancellationToken cancellationToken = default)
    {
        var availableTeacherIds = await _classRepository.GetAvailableTeacherIdsForClassAsync(classId, cancellationToken);

        var teachers = await _userRepository.FindAsync(u => availableTeacherIds.Contains(u.Id), cancellationToken);

        return teachers.Select(t => new AvailableTeacherResponse
        {
            Id = t.Id,
            FullName = t.FullName,
            Email = t.Email,
            Phone = t.Phone,
            Avatar = t.Avatar,
            Gender = t.Gender
        }).ToList();
    }
}

public class AvailableTeacherResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string? Gender { get; set; }
}

public class AvailableStudentsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}

public class AvailableStudentsResponse
{
    public List<StudentResponse> Students { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnavailableCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
