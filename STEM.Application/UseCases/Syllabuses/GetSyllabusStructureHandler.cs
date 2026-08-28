using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using STEM.Application.Dtos.Syllabuses;
using STEM.Core.Entities.Users;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Syllabuses;

public class GetSyllabusStructureHandler
{
    private readonly ISyllabusRepository _syllabusRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetSyllabusStructureHandler(
        ISyllabusRepository syllabusRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _syllabusRepository = syllabusRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<SyllabusStructureResponse?> Handle(
        int syllabusId,
        CancellationToken cancellationToken = default)
    {
        var syllabus = await _syllabusRepository.GetStructureAsync(syllabusId, cancellationToken);
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
