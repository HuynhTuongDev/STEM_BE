using STEM.Application.Dtos.Syllabuses;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Syllabuses;

public class GetSyllabusesListHandler
{
    private readonly ISyllabusRepository _syllabusRepository;

    public GetSyllabusesListHandler(ISyllabusRepository syllabusRepository)
    {
        _syllabusRepository = syllabusRepository;
    }

    public async Task<PagedSyllabusListResponse> Handle(
        GetSyllabusesRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var (syllabuses, totalCount) = await _syllabusRepository.GetSyllabusesPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            request.GradeLevelId,
            request.Status,
            cancellationToken);

        var items = syllabuses.Select(s => new SyllabusListItemResponse
        {
            Id = s.Id,
            Title = s.Title,
            ThumbnailUrl = s.ThumbnailUrl,
            GradeLevelId = s.GradeLevelId,
            GradeLevelName = s.GradeLevel?.Name,
            SubjectArea = s.SubjectArea,
            Status = s.Status,
            DisplayOrder = s.DisplayOrder,
            EstimatedHours = s.EstimatedHours,
            IsRequired = s.IsRequired,
            IsSystemOwned = s.IsSystemOwned,
            CreatedAt = s.CreatedAt
        }).ToList();

        return new PagedSyllabusListResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }
}
