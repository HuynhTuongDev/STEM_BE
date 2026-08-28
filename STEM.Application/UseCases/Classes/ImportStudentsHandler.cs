using STEM.Core.Repository;
using STEM.Core.Entities.Classes;

namespace STEM.Application.UseCases.Classes;

public class ImportStudentsHandler
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public ImportStudentsHandler(
        IClassRepository classRepository,
        IUserRepository userRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<ImportStudentsResult> Handle(int classId, List<int> studentIds, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        var classEntity = await _classRepository.GetByIdAsync(classId);
        if (classEntity == null)
            throw new KeyNotFoundException("Không tìm thấy lớp học.");

        if (classEntity.SchoolId != currentUser.SchoolId && currentUser.Role?.Name != "MasterAdministrator")
            throw new UnauthorizedAccessException("Bạn không có quyền thêm học sinh vào lớp này.");

        var result = new ImportStudentsResult();
        var existingEnrollments = await _enrollmentRepository.GetByClassIdAsync(classId, cancellationToken);
        var alreadyEnrolledIds = existingEnrollments.Select(e => e.StudentId).ToHashSet();

        foreach (var studentId in studentIds)
        {
            if (alreadyEnrolledIds.Contains(studentId))
            {
                result.Failed++;
                result.Errors.Add($"Học sinh ID {studentId} đã có trong lớp.");
                continue;
            }

            // Check course conflict
            var existingCourseEnrollment = await _enrollmentRepository.GetExistingCourseEnrollmentAsync(
                studentId, classEntity.CourseId, classId, cancellationToken);
            if (existingCourseEnrollment != null)
            {
                var student = await _userRepository.GetByIdAsync(studentId);
                result.Failed++;
                result.Errors.Add($"Học sinh \"{student?.FullName ?? studentId.ToString()}\" đã đăng ký khóa học này ở lớp khác.");
                continue;
            }

            // Check schedule conflict
            var canAdd = await _enrollmentRepository.CanAddStudentToClassAsync(studentId, classId, cancellationToken);
            if (!canAdd)
            {
                var student = await _userRepository.GetByIdAsync(studentId);
                result.Failed++;
                result.Errors.Add($"Học sinh \"{student?.FullName ?? studentId.ToString()}\" bị trùng lịch với lớp này.");
                continue;
            }

            var enrollment = new Enrollment
            {
                ClassId = classId,
                StudentId = studentId,
                EnrolledAt = DateTime.UtcNow
            };

            await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
            await _enrollmentRepository.SaveChangesAsync(cancellationToken);
            result.Success++;
        }

        return result;
    }
}

public class ImportStudentsResult
{
    public int Success { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
