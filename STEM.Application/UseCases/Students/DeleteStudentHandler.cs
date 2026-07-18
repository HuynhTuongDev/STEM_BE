using Microsoft.Extensions.Logging;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Students;

public class DeleteStudentHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DeleteStudentHandler> _logger;

    public DeleteStudentHandler(IUserRepository userRepository, ILogger<DeleteStudentHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(int studentId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null || student.Role?.Name != RoleNames.Student)
            return false;

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.SchoolAdministrator)
            throw new UnauthorizedAccessException("Only School Administrator can delete students.");

        if (student.SchoolId != currentUser.SchoolId)
            return false;

        student.IsActive = false;
        _userRepository.Update(student);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft deleted student {StudentId} by user {UserId}", studentId, currentUserId);

        return true;
    }
}
