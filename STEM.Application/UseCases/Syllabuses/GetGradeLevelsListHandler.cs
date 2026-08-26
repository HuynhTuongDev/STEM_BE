using STEM.Application.Dtos.Syllabuses;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Syllabuses;

public class GetGradeLevelsListHandler
{
    private readonly IRepository<GradeLevel> _gradeLevelRepository;

    public GetGradeLevelsListHandler(IRepository<GradeLevel> gradeLevelRepository)
    {
        _gradeLevelRepository = gradeLevelRepository;
    }

    public async Task<List<GradeLevelResponse>> Handle(CancellationToken cancellationToken = default)
    {
        var gradeLevels = await _gradeLevelRepository.GetAllAsync(cancellationToken);

        return gradeLevels
            .OrderBy(g => g.DisplayOrder)
            .ThenBy(g => g.Level)
            .Select(g => new GradeLevelResponse
            {
                Id = g.Id,
                Name = g.Name,
                Code = g.Code,
                DisplayOrder = g.DisplayOrder,
                Level = g.Level
            })
            .ToList();
    }
}
