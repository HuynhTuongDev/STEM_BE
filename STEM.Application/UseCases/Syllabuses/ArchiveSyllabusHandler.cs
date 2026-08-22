using STEM.Application.Interfaces;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Syllabuses;

public class ArchiveSyllabusHandler
{
    private readonly ISyllabusRepository _syllabusRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISystemLogService _systemLogService;

    public ArchiveSyllabusHandler(
        ISyllabusRepository syllabusRepository,
        IUserRepository userRepository,
        ISystemLogService systemLogService)
    {
        _syllabusRepository = syllabusRepository;
        _userRepository = userRepository;
        _systemLogService = systemLogService;
    }

    public async Task<bool> Handle(
        int syllabusId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Only Master Administrator can archive standard syllabuses.");

        var syllabus = await _syllabusRepository.GetByIdAsync(syllabusId, cancellationToken);
        if (syllabus == null)
            return false;

        // Archive only — never hard-delete a syllabus that Courses may reference.
        syllabus.Status = SyllabusStatuses.Archived;
        syllabus.UpdatedAt = DateTime.UtcNow;

        _syllabusRepository.Update(syllabus);
        await _syllabusRepository.SaveChangesAsync(cancellationToken);

        await _systemLogService.WriteAsync(
            SystemLogLevels.Warning,
            SystemLogActions.SyllabusArchived,
            currentUser.Id,
            currentUser.Role?.Name,
            entityType: nameof(Syllabus),
            entityId: syllabus.Id.ToString(),
            description: $"Archived standard syllabus '{syllabus.Title}'.",
            metadata: new { syllabus.Id, syllabus.Title },
            cancellationToken);

        return true;
    }
}
