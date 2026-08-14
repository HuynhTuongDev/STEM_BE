using STEM.Application.Dtos.Classes;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Classes;

public class GetClassDetailHandler
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IRepository<Role> _roleRepository;

    public GetClassDetailHandler(
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

    public async Task<ClassDetailResponse> Handle(
        int classId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        var roleName = currentUser.Role?.Name;

        if (roleName != RoleNames.MasterAdministrator && roleName != RoleNames.SchoolAdministrator && roleName != RoleNames.Teacher)
            throw new UnauthorizedAccessException("Chỉ quản trị viên và giáo viên mới được xem chi tiết lớp học.");

        var classEntity = await _classRepository.GetByIdWithDetailsAsync(classId, cancellationToken);
        if (classEntity == null)
            throw new KeyNotFoundException($"Không tìm thấy lớp học với id {classId}.");

        if (classEntity.SchoolId != currentUser.SchoolId && roleName != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Bạn chỉ có thể xem lớp học thuộc trường của mình.");

        // Get enrolled student IDs
        var enrolledStudentIds = classEntity.Enrollments?.Select(e => e.StudentId).ToHashSet() ?? new HashSet<int>();

        // Get student role
        var studentRole = (await _roleRepository.FindAsync(r => r.Name == "Student", cancellationToken)).FirstOrDefault();

        // Batch query: Get all conflicting student IDs at once
        var conflictingStudentIds = await _enrollmentRepository.GetConflictingStudentIdsAsync(classId, cancellationToken);
        var conflictingSet = conflictingStudentIds.ToHashSet();

        // Get all students in school who are not enrolled and not conflicting
        var availableStudentsList = new List<AvailableStudentResponse>();
        if (studentRole != null)
        {
            var allStudents = await _userRepository.FindAsync(u =>
                u.SchoolId == classEntity.SchoolId &&
                u.RoleId == studentRole.Id, cancellationToken);

            availableStudentsList = allStudents
                .Where(s => !enrolledStudentIds.Contains(s.Id) && !conflictingSet.Contains(s.Id))
                .Select(s => new AvailableStudentResponse
                {
                    Id = s.Id,
                    FullName = s.FullName,
                    Email = s.Email,
                    Phone = s.Phone,
                    Gender = s.Gender
                })
                .ToList();
        }

        return new ClassDetailResponse
        {
            Id = classEntity.Id,
            ClassCode = classEntity.ClassCode,
            SchoolId = classEntity.SchoolId,
            SchoolName = classEntity.School?.Name,
            CourseId = classEntity.CourseId,
            CourseName = classEntity.Course?.Title ?? string.Empty,
            TeacherId = classEntity.TeacherId,
            TeacherName = classEntity.Teacher?.FullName ?? string.Empty,
            StartDate = classEntity.StartDate,
            EndDate = classEntity.EndDate,
            CreatedAt = classEntity.CreatedAt,
            UpdatedAt = classEntity.UpdatedAt,
            Students = classEntity.Enrollments?.Select(e => new StudentResponse
            {
                Id = e.StudentId,
                FullName = e.Student?.FullName ?? string.Empty,
                Email = e.Student?.Email ?? string.Empty,
                EnrolledAt = e.CreatedAt
            }).ToList() ?? new List<StudentResponse>(),
            AvailableStudents = availableStudentsList,
            Schedules = classEntity.Schedules?.Select(s => new ScheduleResponse
            {
                Id = s.Id,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList() ?? new List<ScheduleResponse>(),
            Announcements = classEntity.Announcements?.Select(a => new AnnouncementResponse
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                CreatedAt = a.CreatedAt
            }).ToList() ?? new List<AnnouncementResponse>()
        };
    }

    public async Task<StudentClassDetailResponse> HandleForStudent(
        int classId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        if (student.Role?.Name != RoleNames.Student)
            throw new UnauthorizedAccessException("Chỉ học sinh mới được xem chi tiết lớp học.");

        // Check if student is enrolled in this class
        var enrollment = await _enrollmentRepository.FindAsync(
            e => e.StudentId == studentId && e.ClassId == classId, 
            cancellationToken);
        
        if (!enrollment.Any())
            throw new KeyNotFoundException("Bạn không được đăng ký vào lớp học này.");

        var classEntity = await _classRepository.GetByIdWithDetailsAsync(classId, cancellationToken);
        if (classEntity == null)
            throw new KeyNotFoundException($"Không tìm thấy lớp học với id {classId}.");

        // Get assignments for this class
        var assignments = await _classRepository.GetClassAssignmentsAsync(classId, cancellationToken);

        return new StudentClassDetailResponse
        {
            Id = classEntity.Id,
            ClassCode = classEntity.ClassCode,
            ClassName = classEntity.Course?.Title ?? classEntity.ClassCode,
            CourseName = classEntity.Course?.Title ?? string.Empty,
            TeacherName = classEntity.Teacher?.FullName ?? string.Empty,
            TeacherEmail = classEntity.Teacher?.Email ?? string.Empty,
            Room = string.Empty, // TODO: Get from schedule
            StartDate = classEntity.StartDate,
            EndDate = classEntity.EndDate,
            Progress = CalculateProgress(classEntity),
            Status = DetermineStatus(classEntity),
            Modules = new List<StudentModuleResponse>(), // TODO: Get from course modules
            Assignments = assignments.Select(a => new StudentAssignmentResponse
            {
                Id = a.Id,
                Title = a.Title,
                ClassName = classEntity.Course?.Title ?? string.Empty,
                ClassId = classEntity.Id,
                DueDate = a.DueDate?.ToString("O") ?? string.Empty,
                Status = DetermineAssignmentStatus(a),
                MaxScore = (double)a.MaxScore
            }).ToList()
        };
    }

    public async Task<List<StudentScheduleItemResponse>> HandleGetScheduleForStudent(
        int classId,
        int studentId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        // Check enrollment
        var enrollment = await _enrollmentRepository.FindAsync(
            e => e.StudentId == studentId && e.ClassId == classId, 
            cancellationToken);
        
        if (!enrollment.Any())
            throw new KeyNotFoundException("Bạn không được đăng ký vào lớp học này.");

        var schedules = await _classRepository.GetSchedulesAsync(classId, cancellationToken);
        var now = DateTime.UtcNow;

        return schedules
            .Where(s => !fromDate.HasValue || s.StartTime >= fromDate.Value)
            .Where(s => !toDate.HasValue || s.EndTime <= toDate.Value)
            .Select(s => new StudentScheduleItemResponse
            {
                Id = s.Id,
                Title = "Buổi học",
                Date = s.StartTime.ToString("O"),
                StartTime = s.StartTime.ToString("HH:mm"),
                EndTime = s.EndTime.ToString("HH:mm"),
                Room = string.Empty, // TODO: Get from room
                Status = s.EndTime < now ? "completed" : (s.StartTime <= now ? "ongoing" : "upcoming")
            })
            .OrderBy(s => s.Date)
            .ToList();
    }

    private int CalculateProgress(Class classEntity)
    {
        // Simple progress calculation based on date
        if (classEntity.EndDate < DateTime.UtcNow)
            return 100;
        
        if (classEntity.StartDate > DateTime.UtcNow)
            return 0;

        var totalDays = (classEntity.EndDate - classEntity.StartDate).TotalDays;
        var elapsedDays = (DateTime.UtcNow - classEntity.StartDate).TotalDays;
        
        return totalDays > 0 ? Math.Min(100, (int)(elapsedDays / totalDays * 100)) : 0;
    }

    private string DetermineStatus(Class classEntity)
    {
        if (classEntity.EndDate < DateTime.UtcNow)
            return "completed";
        if (classEntity.StartDate > DateTime.UtcNow)
            return "upcoming";
        return "active";
    }

    private string DetermineAssignmentStatus(Assignment assignment)
    {
        if (assignment.DueDate < DateTime.UtcNow)
            return "overdue";
        return "pending";
    }
}

// Response classes for student
public class StudentClassDetailResponse
{
    public int Id { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Progress { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<StudentModuleResponse> Modules { get; set; } = new();
    public List<StudentAssignmentResponse> Assignments { get; set; } = new();
}

public class StudentModuleResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public int LessonsCompleted { get; set; }
    public int TotalLessons { get; set; }
    public bool IsCompleted { get; set; }
}

public class StudentAssignmentResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double MaxScore { get; set; }
}

public class StudentScheduleItemResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
