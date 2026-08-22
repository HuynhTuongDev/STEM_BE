using STEM.Application.Dtos.Classes;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

using static STEM.Core.Entities.Users.RoleNames;

namespace STEM.Application.UseCases.Classes;

public class RemoveStudentFromClassHandler
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<Enrollment> _enrollmentRepository;

    public RemoveStudentFromClassHandler(
        IClassRepository classRepository,
        IUserRepository userRepository,
        IRepository<Enrollment> enrollmentRepository)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task Handle(int classId, int studentId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        var roleName = currentUser.Role?.Name;

        if (!IsMasterAdmin(roleName) && !IsSchoolAdmin(roleName) && !IsTeacher(roleName))
            throw new UnauthorizedAccessException("Chỉ quản trị viên và giáo viên mới được xóa học sinh khỏi lớp học.");

        var classEntity = await _classRepository.GetByIdAsync(classId, cancellationToken);
        if (classEntity == null)
            throw new KeyNotFoundException("Không tìm thấy lớp học.");

        if (classEntity.SchoolId != currentUser.SchoolId && !IsMasterAdmin(roleName))
            throw new UnauthorizedAccessException("Bạn chỉ có thể thao tác với lớp học thuộc trường của mình.");

        var enrollment = await _enrollmentRepository.FindAsync(e => e.ClassId == classId && e.StudentId == studentId, cancellationToken);
        var target = enrollment.FirstOrDefault();
        if (target == null)
            throw new KeyNotFoundException("Học sinh này chưa được thêm vào lớp học.");

        _enrollmentRepository.Delete(target);
        await _enrollmentRepository.SaveChangesAsync(cancellationToken);
    }
}
