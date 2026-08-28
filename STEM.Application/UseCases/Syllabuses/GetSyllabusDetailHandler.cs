using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using STEM.Application.Dtos.Syllabuses;
using STEM.Core.Entities.Users;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Syllabuses;

public class GetSyllabusDetailHandler
{
    private readonly ISyllabusRepository _syllabusRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetSyllabusDetailHandler(
        ISyllabusRepository syllabusRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _syllabusRepository = syllabusRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<SyllabusDetailResponse?> Handle(
        int syllabusId,
        CancellationToken cancellationToken = default)
    {
        var syllabus = await _syllabusRepository.GetDetailAsync(syllabusId, cancellationToken);
        if (syllabus == null)
            return null;

        var httpContext = _httpContextAccessor.HttpContext;
        var isMasterAdmin = false;
        if (httpContext != null)
        {
            var roleName = httpContext.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? httpContext.User?.FindFirst("role")?.Value
                ?? httpContext.User?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            isMasterAdmin = string.Equals(roleName, RoleNames.MasterAdministrator, StringComparison.OrdinalIgnoreCase);
        }

        if (!isMasterAdmin && syllabus.Status == SyllabusStatuses.Archived)
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
