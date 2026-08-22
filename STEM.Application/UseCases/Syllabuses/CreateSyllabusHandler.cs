using STEM.Application.Dtos.Syllabuses;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Syllabuses;

public class CreateSyllabusHandler
{
    private readonly ISyllabusRepository _syllabusRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<GradeLevel> _gradeLevelRepository;

    public CreateSyllabusHandler(
        ISyllabusRepository syllabusRepository,
        IUserRepository userRepository,
        IRepository<GradeLevel> gradeLevelRepository)
    {
        _syllabusRepository = syllabusRepository;
        _userRepository = userRepository;
        _gradeLevelRepository = gradeLevelRepository;
    }

    public async Task<int> Handle(
        CreateSyllabusRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.MasterAdministrator)
            throw new UnauthorizedAccessException("Only Master Administrator can create standard syllabuses.");

        if (request.GradeLevelId.HasValue)
        {
            var gradeLevelExists = await _gradeLevelRepository.ExistsAsync(request.GradeLevelId.Value, cancellationToken);
            if (!gradeLevelExists)
                throw new InvalidOperationException($"Grade level with ID {request.GradeLevelId.Value} does not exist.");
        }

        var syllabus = new Syllabus
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            ThumbnailUrl = string.IsNullOrWhiteSpace(request.ThumbnailUrl) ? null : request.ThumbnailUrl.Trim(),
            GradeLevelId = request.GradeLevelId,
            SubjectArea = request.SubjectArea?.Trim() ?? string.Empty,
            Status = SyllabusStatuses.Draft,
            DisplayOrder = request.DisplayOrder,
            EstimatedHours = request.EstimatedHours,
            IsRequired = request.IsRequired,
            IsSystemOwned = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _syllabusRepository.AddAsync(syllabus, cancellationToken);
        await _syllabusRepository.SaveChangesAsync(cancellationToken);

        return syllabus.Id;
    }
}
