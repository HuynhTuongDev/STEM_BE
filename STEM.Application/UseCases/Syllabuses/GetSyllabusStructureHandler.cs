using STEM.Application.Dtos.Syllabuses;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Syllabuses;

public class GetSyllabusStructureHandler
{
    private readonly ISyllabusRepository _syllabusRepository;

    public GetSyllabusStructureHandler(ISyllabusRepository syllabusRepository)
    {
        _syllabusRepository = syllabusRepository;
    }

    public async Task<SyllabusStructureResponse?> Handle(
        int syllabusId,
        CancellationToken cancellationToken = default)
    {
        var syllabus = await _syllabusRepository.GetStructureAsync(syllabusId, cancellationToken);
        if (syllabus == null)
            return null;

        return new SyllabusStructureResponse
        {
            Id = syllabus.Id,
            Title = syllabus.Title,
            Status = syllabus.Status,
            Courses = syllabus.Courses
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new SyllabusStructureCourseNode
                {
                    Id = c.Id,
                    Title = c.Title,
                    SchoolId = c.SchoolId,
                    DisplayOrder = c.DisplayOrder,
                    Modules = c.Modules
                        .OrderBy(m => m.DisplayOrder)
                        .Select(m => new SyllabusStructureModuleNode
                        {
                            Id = m.Id,
                            Title = m.Title,
                            DisplayOrder = m.DisplayOrder,
                            Lessons = m.Lessons
                                .OrderBy(l => l.DisplayOrder)
                                .Select(l => new SyllabusStructureLessonNode
                                {
                                    Id = l.Id,
                                    Title = l.Title,
                                    DisplayOrder = l.DisplayOrder,
                                    HasVirtualLab = l.HasVirtualLab,
                                    Lab = l.Lab == null ? null : new SyllabusStructureLabSummary
                                    {
                                        Id = l.Lab.Id,
                                        Title = l.Lab.Title,
                                        Status = l.Lab.Status
                                    }
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
