using STEM.Application.Dtos.Syllabuses;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Syllabuses;

public class GetSyllabusDetailHandler
{
    private readonly ISyllabusRepository _syllabusRepository;

    public GetSyllabusDetailHandler(ISyllabusRepository syllabusRepository)
    {
        _syllabusRepository = syllabusRepository;
    }

    public async Task<SyllabusDetailResponse?> Handle(
        int syllabusId,
        CancellationToken cancellationToken = default)
    {
        var syllabus = await _syllabusRepository.GetDetailAsync(syllabusId, cancellationToken);
        if (syllabus == null)
            return null;

        return new SyllabusDetailResponse
        {
            Id = syllabus.Id,
            Title = syllabus.Title,
            Description = syllabus.Description,
            ThumbnailUrl = syllabus.ThumbnailUrl,
            GradeLevelId = syllabus.GradeLevelId,
            GradeLevelName = syllabus.GradeLevel?.Name,
            SubjectArea = syllabus.SubjectArea,
            Status = syllabus.Status,
            DisplayOrder = syllabus.DisplayOrder,
            EstimatedHours = syllabus.EstimatedHours,
            IsRequired = syllabus.IsRequired,
            IsSystemOwned = syllabus.IsSystemOwned,
            CreatedAt = syllabus.CreatedAt,
            UpdatedAt = syllabus.UpdatedAt
        };
    }
}
