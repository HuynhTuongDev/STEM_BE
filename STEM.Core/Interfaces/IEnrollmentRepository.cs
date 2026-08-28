using STEM.Core.Entities.Classes;

namespace STEM.Core.Repository;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<IEnumerable<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Enrollment>> GetByClassIdAsync(int classId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StudentScheduleConflict>> GetConflictingStudentsAsync(int classId, CancellationToken cancellationToken = default);
    Task<List<int>> GetConflictingStudentIdsAsync(int classId, CancellationToken cancellationToken = default);
    Task<bool> CanAddStudentToClassAsync(int studentId, int classId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra xem học sinh đã được enroll vào lớp khác cùng course chưa
    /// </summary>
    Task<StudentCourseEnrollment?> GetExistingCourseEnrollmentAsync(int studentId, int courseId, int excludeClassId, CancellationToken cancellationToken = default);
}

public class StudentCourseEnrollment
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
}