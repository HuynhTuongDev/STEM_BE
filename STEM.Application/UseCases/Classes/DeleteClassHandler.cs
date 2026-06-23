using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Classes;

public class DeleteClassHandler
{
    private readonly IClassRepository _classRepository;
    private readonly IUserRepository _userRepository;

    public DeleteClassHandler(IClassRepository classRepository, IUserRepository userRepository)
    {
        _classRepository = classRepository;
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(
        int classId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Người dùng không tồn tại.");

        if (currentUser.Role?.Name != RoleNames.SchoolAdministrator && currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Chỉ Quản trị viên trường mới được xóa lớp học.");

        var classEntity = await _classRepository.GetByIdAsync(classId, cancellationToken);
        if (classEntity == null)
            return false;

        if (classEntity.SchoolId != currentUser.SchoolId && currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Bạn chỉ có thể xóa lớp học thuộc trường của mình.");

        _classRepository.Delete(classEntity);
        await _classRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
