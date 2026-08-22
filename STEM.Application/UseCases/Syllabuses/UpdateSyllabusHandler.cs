using STEM.Application.Dtos.Syllabuses;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Common;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Syllabuses;

public class UpdateSyllabusHandler
{
    private readonly ISyllabusRepository _syllabusRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<GradeLevel> _gradeLevelRepository;
    private readonly ISystemLogService _systemLogService;

    public UpdateSyllabusHandler(
        ISyllabusRepository syllabusRepository,
        IUserRepository userRepository,
        IRepository<GradeLevel> gradeLevelRepository,
        ISystemLogService systemLogService)
    {
        _syllabusRepository = syllabusRepository;
        _userRepository = userRepository;
        _gradeLevelRepository = gradeLevelRepository;
        _systemLogService = systemLogService;
    }

    public async Task<bool> Handle(
        int syllabusId,
        UpdateSyllabusRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        if (!string.IsNullOrWhiteSpace(request.Status) && !SyllabusStatuses.All.Contains(request.Status))
            throw new ArgumentException($"Invalid status '{request.Status}'.");

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Only Master Administrator can update standard syllabuses.");

        var syllabus = await _syllabusRepository.GetByIdAsync(syllabusId, cancellationToken);
        if (syllabus == null)
            return false;

        if (request.GradeLevelId.HasValue)
        {
            var gradeLevelExists = await _gradeLevelRepository.ExistsAsync(request.GradeLevelId.Value, cancellationToken);
            if (!gradeLevelExists)
                throw new InvalidOperationException($"Grade level with ID {request.GradeLevelId.Value} does not exist.");
        }

        syllabus.Title = request.Title.Trim();
        syllabus.Description = request.Description?.Trim() ?? string.Empty;
        syllabus.ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl) ? null : request.ThumbnailUrl.Trim();
        syllabus.GradeLevelId = request.GradeLevelId;
        syllabus.SubjectArea = request.SubjectArea?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(request.Status))
            syllabus.Status = request.Status;
        syllabus.DisplayOrder = request.DisplayOrder;
        syllabus.EstimatedHours = request.EstimatedHours;
        syllabus.IsRequired = request.IsRequired;
        syllabus.UpdatedAt = DateTime.UtcNow;

        _syllabusRepository.Update(syllabus);
        await _syllabusRepository.SaveChangesAsync(cancellationToken);

        await _systemLogService.WriteAsync(
            SystemLogLevels.Information,
            SystemLogActions.SyllabusUpdated,
            currentUser.Id,
            currentUser.Role?.Name,
            entityType: nameof(Syllabus),
            entityId: syllabus.Id.ToString(),
            description: $"Updated standard syllabus '{syllabus.Title}'.",
            metadata: new { syllabus.Id, syllabus.Title, syllabus.Status },
            cancellationToken);

        return true;
    }
}
