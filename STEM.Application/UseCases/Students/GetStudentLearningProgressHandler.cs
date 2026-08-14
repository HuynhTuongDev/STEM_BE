using STEM.Application.Dtos.Students;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Participants;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using ClassEntity = STEM.Core.Entities.Classes.Class;
using CourseModule = STEM.Core.Entities.Courses.Module;

namespace STEM.Application.UseCases.Students;

public class GetStudentLearningProgressHandler
{
    private const string StudentRoleName = "Student";

    private readonly IUserRepository _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Enrollment> _enrollmentRepository;
    private readonly IRepository<ClassEntity> _classRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly IRepository<CourseModule> _moduleRepository;
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IRepository<Assignment> _assignmentRepository;
    private readonly IRepository<ProjectMember> _projectMemberRepository;
    private readonly IRepository<SimulationSession> _simulationSessionRepository;
    private readonly ISubmissionRepository _submissionRepository;

    public GetStudentLearningProgressHandler(
        IUserRepository userRepository,
        IRepository<Role> roleRepository,
        IRepository<Enrollment> enrollmentRepository,
        IRepository<ClassEntity> classRepository,
        IRepository<Course> courseRepository,
        IRepository<CourseModule> moduleRepository,
        IRepository<Lesson> lessonRepository,
        IRepository<Assignment> assignmentRepository,
        IRepository<ProjectMember> projectMemberRepository,
        IRepository<SimulationSession> simulationSessionRepository,
        ISubmissionRepository submissionRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _enrollmentRepository = enrollmentRepository;
        _classRepository = classRepository;
        _courseRepository = courseRepository;
        _moduleRepository = moduleRepository;
        _lessonRepository = lessonRepository;
        _assignmentRepository = assignmentRepository;
        _projectMemberRepository = projectMemberRepository;
        _simulationSessionRepository = simulationSessionRepository;
        _submissionRepository = submissionRepository;
    }

    public async Task<StudentLearningProgressResponse> Handle(int studentId, CancellationToken cancellationToken = default)
    {
        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        var studentRole = (await _roleRepository.FindAsync(role => role.Name == StudentRoleName, cancellationToken))
            .FirstOrDefault();

        if (student == null || studentRole == null || student.RoleId != studentRole.Id)
        {
            throw new KeyNotFoundException("Student not found.");
        }

        var enrollments = (await _enrollmentRepository.FindAsync(enrollment => enrollment.StudentId == studentId, cancellationToken))
            .ToList();
        var classIds = enrollments.Select(enrollment => enrollment.ClassId).Distinct().ToList();

        var classes = classIds.Count == 0
            ? new List<ClassEntity>()
            : (await _classRepository.FindAsync(classEntity => classIds.Contains(classEntity.Id), cancellationToken)).ToList();
        var courseIds = classes.Select(classEntity => classEntity.CourseId).Distinct().ToList();

        var courses = courseIds.Count == 0
            ? new List<Course>()
            : (await _courseRepository.FindAsync(course => courseIds.Contains(course.Id), cancellationToken)).ToList();
        var modules = courseIds.Count == 0
            ? new List<CourseModule>()
            : (await _moduleRepository.FindAsync(module => courseIds.Contains(module.CourseId), cancellationToken)).ToList();
        var moduleIds = modules.Select(module => module.Id).Distinct().ToList();

        var lessons = moduleIds.Count == 0
            ? new List<Lesson>()
            : (await _lessonRepository.FindAsync(lesson => moduleIds.Contains(lesson.ModuleId), cancellationToken)).ToList();
        var assignments = classIds.Count == 0
            ? new List<Assignment>()
            : (await _assignmentRepository.FindAsync(assignment => classIds.Contains(assignment.ClassId), cancellationToken)).ToList();
        var projectMembers = (await _projectMemberRepository.FindAsync(projectMember => projectMember.StudentId == studentId, cancellationToken))
            .ToList();
        var simulationSessions = (await _simulationSessionRepository.FindAsync(session => session.StudentId == studentId, cancellationToken))
            .ToList();

        var courseById = courses.ToDictionary(course => course.Id);
        var lessonCountsByCourse = lessons
            .Join(
                modules,
                lesson => lesson.ModuleId,
                module => module.Id,
                (_, module) => module.CourseId)
            .GroupBy(courseId => courseId)
            .ToDictionary(group => group.Key, group => group.Count());
        var assignmentCountsByClass = assignments
            .GroupBy(assignment => assignment.ClassId)
            .ToDictionary(group => group.Key, group => group.Count());

        var now = DateTime.UtcNow;
        var classProgress = classes
            .OrderBy(classEntity => classEntity.StartDate)
            .ThenBy(classEntity => classEntity.Id)
            .Select(classEntity =>
            {
                courseById.TryGetValue(classEntity.CourseId, out var course);

                return new StudentClassProgressResponse
                {
                    ClassId = classEntity.Id,
                    CourseId = classEntity.CourseId,
                    CourseTitle = course?.Title ?? string.Empty,
                    TeacherId = classEntity.TeacherId,
                    StartDate = classEntity.StartDate,
                    EndDate = classEntity.EndDate,
                    Status = GetClassStatus(classEntity, now),
                    TotalLessons = lessonCountsByCourse.GetValueOrDefault(classEntity.CourseId),
                    TotalAssignments = assignmentCountsByClass.GetValueOrDefault(classEntity.Id),
                    HasCertificate = false,
                    TotalAttendanceRecords = 0,
                    PresentAttendanceRecords = 0,
                    AttendanceRate = 0
                };
            })
            .ToList();

        // Calculate grades and average (only Report and Lab)
        var gradedSubmissions = await _submissionRepository.GetGradedByStudentIdAsync(studentId, cancellationToken);
        var recentGrades = gradedSubmissions
            .OrderByDescending(s => s.GradedAt)
            .Take(5)
            .Select(s => new StudentGradeResponse
            {
                AssignmentId = s.AssignmentId,
                AssignmentTitle = s.Assignment?.Title ?? string.Empty,
                Score = s.FinalScore ?? 0,
                MaxScore = s.Assignment?.MaxScore ?? 100,
                GradedAt = s.GradedAt ?? DateTime.UtcNow
            })
            .ToList();

        var totalGrades = gradedSubmissions.Count();
        var averageScore = totalGrades > 0
            ? Math.Round((double)gradedSubmissions.Average(s => s.FinalScore ?? 0), 2)
            : (double?)null;

        return new StudentLearningProgressResponse
        {
            StudentId = student.Id,
            FullName = student.FullName,
            Email = student.Email,
            TotalEnrolledClasses = classIds.Count,
            ActiveClassCount = classes.Count(classEntity => classEntity.StartDate <= now && classEntity.EndDate >= now),
            TotalCourses = courseIds.Count,
            CompletedCourses = 0,
            CourseCompletionRate = 0,
            TotalLessons = lessons.Count,
            TotalAssignments = assignments.Count,
            TotalProjects = projectMembers.Select(projectMember => projectMember.ProjectId).Distinct().Count(),
            TotalSimulationSessions = simulationSessions.Count,
            TotalGrades = totalGrades,
            AverageScore = averageScore,
            CertificatesEarned = 0,
            TotalAttendanceRecords = 0,
            PresentAttendanceRecords = 0,
            AttendanceRate = 0,
            Classes = classProgress,
            RecentGrades = recentGrades
        };
    }

    private static decimal CalculateRate(int value, int total)
    {
        return total == 0 ? 0 : Math.Round(value * 100m / total, 2);
    }

    private static string GetClassStatus(ClassEntity classEntity, DateTime now)
    {
        if (classEntity.StartDate > now)
        {
            return "NotStarted";
        }

        if (classEntity.EndDate < now)
        {
            return "Completed";
        }

        return "InProgress";
    }
}
