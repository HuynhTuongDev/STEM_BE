using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using STEM.Application.Dtos.Syllabuses;
using STEM.Core.Entities.Users;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Syllabuses;

public class GetSyllabusesListHandler
{
    private readonly ISyllabusRepository _syllabusRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetSyllabusesListHandler(
        ISyllabusRepository syllabusRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _syllabusRepository = syllabusRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedSyllabusListResponse> Handle(
        GetSyllabusesRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var isMasterAdmin = false;
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var roleName = httpContext.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? httpContext.User?.FindFirst("role")?.Value
                ?? httpContext.User?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            isMasterAdmin = string.Equals(roleName, RoleNames.MasterAdministrator, StringComparison.OrdinalIgnoreCase);
        }

        if (!isMasterAdmin && string.Equals(request.Status, SyllabusStatuses.Archived, StringComparison.OrdinalIgnoreCase))
        {
            return new PagedSyllabusListResponse();
        }

        var (syllabuses, totalCount) = await _syllabusRepository.GetSyllabusesPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            request.GradeLevelId,
            request.Status,
            excludeArchived: !isMasterAdmin,
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
