using STEM.Application.Dtos.Classes;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Classes;

public class AssignStudentsToClassHandler
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<Enrollment> _enrollmentRepository;

    public AssignStudentsToClassHandler(
        IClassRepository classRepository,
        IUserRepository userRepository,
        IRepository<Enrollment> enrollmentRepository)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task Handle(int classId, AssignStudentsRequest request, int currentUserId)
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

        if (classEntity.SchoolId != currentUser.SchoolId && currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác với lớp học này.");

        var validStudents = (await _userRepository.FindAsync(u => request.StudentIds.Contains(u.Id))).ToList();
        if (validStudents.Count != request.StudentIds.Count)
        {
            throw new ArgumentException("Một số học sinh không tồn tại.");
        }

        foreach (var studentId in request.StudentIds)
        {
            var exists = await _enrollmentRepository.FindAsync(e => e.ClassId == classId && e.StudentId == studentId);
            if (exists.Any())
            {
                continue;
            }

            var enrollment = new Enrollment
            {
                ClassId = classId,
                StudentId = studentId,
            };

            await _enrollmentRepository.AddAsync(enrollment);
        }

        await _enrollmentRepository.SaveChangesAsync();
    }
}
