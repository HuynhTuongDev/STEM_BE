using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Users;

public class DeleteTeacherHandler
{
    private readonly IUserRepository _userRepository;

    public DeleteTeacherHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(
        int teacherId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.SchoolAdministrator)
            throw new UnauthorizedAccessException("Only School Administrator can delete teachers.");

        var teacher = await _userRepository.GetByIdAsync(teacherId, cancellationToken);
        if (teacher == null)
            return false;

        if (teacher.Role?.Name != RoleNames.Teacher)
            throw new InvalidOperationException("The specified user is not a teacher.");

        if (teacher.SchoolId != currentUser.SchoolId)
            throw new UnauthorizedAccessException("You can only delete teachers from your own school.");

        _userRepository.Delete(teacher);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
