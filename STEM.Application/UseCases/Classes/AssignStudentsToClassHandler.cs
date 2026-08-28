using STEM.Application.Dtos.Classes;
using STEM.Application.Dtos.Schedules;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Application.Interfaces;
using System.Linq;

namespace STEM.Application.UseCases.Classes;

public class AssignStudentsToClassHandler
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<Enrollment> _enrollmentRepository;
    private readonly IEnrollmentRepository _enrollmentFullRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly INotificationService _notificationService;

    public AssignStudentsToClassHandler(
        IClassRepository classRepository,
        IUserRepository userRepository,
        IRepository<Enrollment> enrollmentRepository,
        IEnrollmentRepository enrollmentFullRepository,
        IAttendanceRepository attendanceRepository,
        INotificationService notificationService)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
        _enrollmentRepository = enrollmentRepository;
        _enrollmentFullRepository = enrollmentFullRepository;
        _attendanceRepository = attendanceRepository;
        _notificationService = notificationService;
    }

    public async Task<AssignStudentsResponse> Handle(int classId, AssignStudentsRequest request, int currentUserId)
    {
        if (request.StudentIds == null || !request.StudentIds.Any())
        {
            throw new ArgumentException("Danh sách học sinh không được để trống.");
        }

        var currentUser = await _userRepository.GetByIdAsync(currentUserId);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        var classEntity = await _classRepository.GetByIdAsync(classId);
        if (classEntity == null)
            throw new KeyNotFoundException("Không tìm thấy lớp học.");

        // Get class with course details for notification
        var classWithCourse = await _classRepository.GetByIdWithDetailsAsync(classId);

        if (classEntity.SchoolId != currentUser.SchoolId && currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác với lớp học này.");

        // Validate all students exist in ONE query
        var validStudents = await _userRepository.FindAsync(u => request.StudentIds.Contains(u.Id));
        var validStudentIds = validStudents.Select(u => u.Id).ToHashSet();

        var invalidIds = request.StudentIds.Except(validStudentIds).ToList();
        if (invalidIds.Any())
        {
            throw new ArgumentException($"Học sinh không tồn tại: {string.Join(", ", invalidIds)}");
        }

        // Get already enrolled students in ONE query
        var existingEnrollments = await _enrollmentRepository.FindAsync(e => e.ClassId == classId);
        var existingEnrollmentStudentIds = existingEnrollments
            .Where(e => request.StudentIds.Contains(e.StudentId))
            .Select(e => e.StudentId)
            .ToHashSet();

        // Check for same-course enrollment conflicts (ALWAYS reject)
        var courseConflictStudents = new List<StudentCourseEnrollment>();
        var studentsToCheck = request.StudentIds.Except(existingEnrollmentStudentIds).ToList();

        foreach (var studentId in studentsToCheck)
        {
            var existingCourseEnrollment = await _enrollmentFullRepository.GetExistingCourseEnrollmentAsync(
                studentId, classEntity.CourseId, classId);

            if (existingCourseEnrollment != null)
            {
                courseConflictStudents.Add(existingCourseEnrollment);
            }
        }

        // ALWAYS reject if any course conflicts exist
        if (courseConflictStudents.Any())
        {
            var conflictInfo = string.Join(", ", courseConflictStudents.Select(c =>
                $"{validStudents.First(s => s.Id == c.StudentId).FullName} (đã học {c.CourseName} tại lớp {c.ClassCode})"));
            throw new ArgumentException($"Học sinh đã học khóa học này tại lớp khác: {conflictInfo}");
        }

        // Check schedule conflicts for students NOT already enrolled and NOT course-conflicted
        var courseConflictStudentIds = courseConflictStudents.Select(c => c.StudentId).ToHashSet();
        var studentsForScheduleCheck = studentsToCheck.Except(courseConflictStudentIds).ToList();
        var conflictingStudents = new List<StudentScheduleConflict>();

        foreach (var studentId in studentsForScheduleCheck)
        {
            var canAdd = await _enrollmentFullRepository.CanAddStudentToClassAsync(studentId, classId);
            if (!canAdd)
            {
                var student = validStudents.First(u => u.Id == studentId);
                // Get the conflicting schedule info
                var conflicts = await _enrollmentFullRepository.GetConflictingStudentsAsync(classId);
                var studentConflict = conflicts.FirstOrDefault(c => c.StudentId == studentId);
                if (studentConflict != null)
                {
                    conflictingStudents.Add(studentConflict);
                }
                else
                {
                    // Fallback: add basic conflict info
                    conflictingStudents.Add(new StudentScheduleConflict
                    {
                        StudentId = studentId,
                        StudentName = student.FullName,
                        StudentEmail = student.Email,
                        ConflictingClassId = 0,
                        ConflictingClassCode = "(trùng lịch)",
                        ConflictingStartTime = DateTime.MinValue,
                        ConflictingEndTime = DateTime.MinValue
                    });
                }
            }
        }

        // If strict mode is enabled, reject if any schedule conflicts exist
        if (request.StrictMode && conflictingStudents.Any())
        {
            throw new ArgumentException($"Có {conflictingStudents.Count} học sinh bị trùng lịch: {string.Join(", ", conflictingStudents.Select(c => c.StudentName))}");
        }

        // Calculate new enrollments (exclude already enrolled and schedule-conflicted if not strict mode)
        var conflictingStudentIds = conflictingStudents.Select(c => c.StudentId).ToHashSet();
        var newStudentIds = studentsToCheck
            .Except(conflictingStudentIds)
            .ToList();

        if (newStudentIds.Any())
        {
            // Batch insert - MUCH faster than individual inserts
            var now = DateTime.UtcNow;
            var enrollments = newStudentIds.Select(studentId => new Enrollment
            {
                ClassId = classId,
                StudentId = studentId,
                EnrolledAt = now
            }).ToList();

            await _enrollmentRepository.AddRangeAsync(enrollments);
            await _enrollmentRepository.SaveChangesAsync();

            // Create attendance records for all existing schedules in this class
            var schedules = classWithCourse?.Schedules?.ToList() ?? new List<Schedule>();
            if (schedules.Any())
            {
                // Check for existing attendance records to avoid duplicates
                var existingAttendance = await _attendanceRepository.FindAsync(
                    a => a.ClassId == classId && newStudentIds.Contains(a.StudentId));
                var existingKeys = existingAttendance
                    .Select(a => (a.StudentId, ScheduleId: a.ScheduleId ?? 0))
                    .ToHashSet();

                var nowForAttendance = DateTime.UtcNow;
                var attendanceRecords = newStudentIds
                    .SelectMany(studentId => schedules.Select(schedule => new
                    {
                        StudentId = studentId,
                        Schedule = schedule,
                        Key = (studentId, ScheduleId: schedule.Id)
                    }))
                    .Where(x => !existingKeys.Contains(x.Key))
                    .Select(x => new AttendanceRecord
                    {
                        ClassId = classId,
                        ScheduleId = x.Schedule.Id,
                        StudentId = x.StudentId,
                        AttendanceDate = DateOnly.FromDateTime(x.Schedule.StartTime),
                        Status = null,
                        CreatedAt = nowForAttendance,
                        UpdatedAt = nowForAttendance
                    })
                    .ToList();

                if (attendanceRecords.Any())
                {
                    await _attendanceRepository.AddRangeAsync(attendanceRecords);
                    await _attendanceRepository.SaveChangesAsync();
                }
            }

            // N-15: Notify newly added students
            if (classWithCourse?.Course != null)
            {
                var courseName = classWithCourse.Course.Title ?? "khóa học";
                var title = $"Bạn đã được thêm vào lớp {classWithCourse.ClassCode}";
                var content = $"Bạn đã được thêm vào lớp {classWithCourse.ClassCode} - {courseName}.";

                await _notificationService.SendToManyAsync(newStudentIds, title, content, NotificationType.AddedToClass);
            }
        }

        // Get full student info for response
        var newStudentEntities = validStudents.Where(u => newStudentIds.Contains(u.Id));
        var addedStudents = newStudentEntities.Select(u => new StudentResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            EnrolledAt = DateTime.UtcNow
        }).ToList();

        return new AssignStudentsResponse
        {
            TotalRequested = request.StudentIds.Count,
            SuccessCount = newStudentIds.Count,
            AlreadyEnrolledCount = existingEnrollmentStudentIds.Count,
            ConflictCount = conflictingStudents.Count + courseConflictStudents.Count,
            AlreadyEnrolledStudentIds = existingEnrollmentStudentIds.ToList(),
            AddedStudents = addedStudents,
            ConflictingStudents = conflictingStudents
        };
    }
}
