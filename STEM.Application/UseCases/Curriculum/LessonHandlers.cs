using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Linq;
using STEM.Application.Dtos.Curriculum;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Entities.Users;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Curriculum;

public class GetLessonsHandler
{
    private readonly ILessonRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetLessonsHandler(
        ILessonRepository repository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<LessonDto>> Handle(int moduleId, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        string? roleName = null;
        if (httpContext != null)
        {
            roleName = httpContext.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? httpContext.User?.FindFirst("role")?.Value
                ?? httpContext.User?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        }

        var isStudentOrTeacher = string.Equals(roleName, RoleNames.Student, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, RoleNames.Teacher, StringComparison.OrdinalIgnoreCase);
        if (isStudentOrTeacher)
        {
            var isRestricted = await _repository.IsSyllabusOrCourseRestrictedForModuleAsync(moduleId, cancellationToken);
            if (isRestricted)
            {
                return Enumerable.Empty<LessonDto>();
            }
        }
        else
        {
            var isMasterAdmin = string.Equals(roleName, RoleNames.MasterAdministrator, StringComparison.OrdinalIgnoreCase);
            if (!isMasterAdmin)
            {
                var isArchived = await _repository.IsSyllabusArchivedForModuleAsync(moduleId, cancellationToken);
                if (isArchived)
                {
                    return Enumerable.Empty<LessonDto>();
                }
            }
        }

        var lessons = await _repository.GetByModuleIdOrderedAsync(moduleId);
        
        return lessons.Select(l => new LessonDto
        {
            Id = l.Id,
            ModuleId = l.ModuleId,
            Title = l.Title,
            Description = l.Description,
            Content = l.Content,
            Input = l.Input,
            Output = l.Output,
            DisplayOrder = l.DisplayOrder,
            EstimatedMinutes = l.EstimatedMinutes,
            LessonType = l.LessonType,
            HasVirtualLab = l.HasVirtualLab,
            LabId = l.LabId,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        });
    }

    public async Task<IEnumerable<LessonDto>> HandleByClass(int classId, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        string? roleName = null;
        if (httpContext != null)
        {
            roleName = httpContext.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? httpContext.User?.FindFirst("role")?.Value
                ?? httpContext.User?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        }

        var isStudentOrTeacher = string.Equals(roleName, RoleNames.Student, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, RoleNames.Teacher, StringComparison.OrdinalIgnoreCase);
        if (isStudentOrTeacher)
        {
            var isRestricted = await _repository.IsSyllabusOrCourseRestrictedForClassAsync(classId, cancellationToken);
            if (isRestricted)
            {
                return Enumerable.Empty<LessonDto>();
            }
        }
        else
        {
            var isMasterAdmin = string.Equals(roleName, RoleNames.MasterAdministrator, StringComparison.OrdinalIgnoreCase);
            if (!isMasterAdmin)
            {
                var isArchived = await _repository.IsSyllabusArchivedForClassAsync(classId, cancellationToken);
                if (isArchived)
                {
                    return Enumerable.Empty<LessonDto>();
                }
            }
        }

        var lessons = await _repository.GetByClassIdAsync(classId);
        
        return lessons.Select(l => new LessonDto
        {
            Id = l.Id,
            ModuleId = l.ModuleId,
            Title = l.Title,
            Description = l.Description,
            Content = l.Content,
            Input = l.Input,
            Output = l.Output,
            DisplayOrder = l.DisplayOrder,
            EstimatedMinutes = l.EstimatedMinutes,
            LessonType = l.LessonType,
            HasVirtualLab = l.HasVirtualLab,
            LabId = l.LabId,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        });
    }
}

public class GetLessonByIdHandler
{
    private readonly ILessonRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetLessonByIdHandler(
        ILessonRepository repository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LessonDto?> Handle(int id, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        string? roleName = null;
        if (httpContext != null)
        {
            roleName = httpContext.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? httpContext.User?.FindFirst("role")?.Value
                ?? httpContext.User?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
        }

        var isStudentOrTeacher = string.Equals(roleName, RoleNames.Student, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, RoleNames.Teacher, StringComparison.OrdinalIgnoreCase);
        if (isStudentOrTeacher)
        {
            var isRestricted = await _repository.IsSyllabusOrCourseRestrictedForLessonAsync(id, cancellationToken);
            if (isRestricted)
            {
                return null;
            }
        }
        else
        {
            var isMasterAdmin = string.Equals(roleName, RoleNames.MasterAdministrator, StringComparison.OrdinalIgnoreCase);
            if (!isMasterAdmin)
            {
                var isArchived = await _repository.IsSyllabusArchivedForLessonAsync(id, cancellationToken);
                if (isArchived)
                {
                    return null;
                }
            }
        }

        var lesson = await _repository.GetByIdAsync(id);
        if (lesson == null)
            return null;

        return new LessonDto
        {
            Id = lesson.Id,
            ModuleId = lesson.ModuleId,
            Title = lesson.Title,
            Description = lesson.Description,
            Content = lesson.Content,
            Input = lesson.Input,
            Output = lesson.Output,
            DisplayOrder = lesson.DisplayOrder,
            EstimatedMinutes = lesson.EstimatedMinutes,
            LessonType = lesson.LessonType,
            HasVirtualLab = lesson.HasVirtualLab,
            LabId = lesson.LabId,
            CreatedAt = lesson.CreatedAt,
            UpdatedAt = lesson.UpdatedAt
        };
    }
}

public class CreateLessonHandler
{
    private readonly ILessonRepository _repository;
    private readonly IModuleRepository _moduleRepository;
    private readonly ICourseRepository _courseRepository;

    public CreateLessonHandler(
        ILessonRepository lessonRepository,
        IModuleRepository moduleRepository,
        ICourseRepository courseRepository)
    {
        _repository = lessonRepository;
        _moduleRepository = moduleRepository;
        _courseRepository = courseRepository;
    }

    public async Task<int> Handle(CreateLessonRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var module = await _moduleRepository.GetByIdAsync(request.ModuleId);
        if (module == null)
            throw new InvalidOperationException($"Module with ID {request.ModuleId} does not exist.");

        var course = await _courseRepository.GetByIdAsync(module.CourseId);
        if (course != null && course.Status != SyllabusStatuses.Draft)
            throw new InvalidOperationException("Only lessons in Draft courses can be added.");

        var lesson = new Lesson
        {
            ModuleId = request.ModuleId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Content = request.Content?.Trim() ?? string.Empty,
            Input = request.Input?.Trim() ?? string.Empty,
            Output = request.Output?.Trim() ?? string.Empty,
            DisplayOrder = request.DisplayOrder,
            EstimatedMinutes = request.EstimatedMinutes,
            LessonType = request.LessonType,
            HasVirtualLab = request.HasVirtualLab,
            LabId = request.LabId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(lesson);
        return lesson.Id;
    }
}

public class UpdateLessonHandler
{
    private readonly ILessonRepository _repository;
    private readonly IModuleRepository _moduleRepository;
    private readonly ICourseRepository _courseRepository;

    public UpdateLessonHandler(
        ILessonRepository lessonRepository,
        IModuleRepository moduleRepository,
        ICourseRepository courseRepository)
    {
        _repository = lessonRepository;
        _moduleRepository = moduleRepository;
        _courseRepository = courseRepository;
    }

    public async Task<bool> Handle(int id, UpdateLessonRequest request, CancellationToken cancellationToken = default)
    {
        var lesson = await _repository.GetByIdAsync(id);
        if (lesson == null)
            return false;

        var module = await _moduleRepository.GetByIdAsync(lesson.ModuleId);
        if (module != null)
        {
            var course = await _courseRepository.GetByIdAsync(module.CourseId);
            if (course != null && course.Status != SyllabusStatuses.Draft)
                throw new InvalidOperationException("Only lessons in Draft courses can be edited.");
        }

        lesson.Title = request.Title.Trim();
        lesson.Description = request.Description?.Trim() ?? string.Empty;
        lesson.Content = request.Content?.Trim() ?? string.Empty;
        lesson.Input = request.Input?.Trim() ?? string.Empty;
        lesson.Output = request.Output?.Trim() ?? string.Empty;
        lesson.DisplayOrder = request.DisplayOrder;
        lesson.EstimatedMinutes = request.EstimatedMinutes;
        lesson.LessonType = request.LessonType;
        lesson.HasVirtualLab = request.HasVirtualLab;
        lesson.LabId = request.LabId;
        lesson.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(lesson);
        return true;
    }
}

public class DeleteLessonHandler
{
    private readonly ILessonRepository _repository;
    private readonly IModuleRepository _moduleRepository;
    private readonly ICourseRepository _courseRepository;

    public DeleteLessonHandler(
        ILessonRepository lessonRepository,
        IModuleRepository moduleRepository,
        ICourseRepository courseRepository)
    {
        _repository = lessonRepository;
        _moduleRepository = moduleRepository;
        _courseRepository = courseRepository;
    }

    public async Task<bool> Handle(int id, CancellationToken cancellationToken = default)
    {
        var lesson = await _repository.GetByIdAsync(id);
        if (lesson == null)
            return false;

        var module = await _moduleRepository.GetByIdAsync(lesson.ModuleId);
        if (module != null)
        {
            var course = await _courseRepository.GetByIdAsync(module.CourseId);
            if (course != null && course.Status != SyllabusStatuses.Draft)
                throw new InvalidOperationException("Only lessons in Draft courses can be deleted.");
        }

        return await _repository.DeleteAsync(id);
    }
}

public class ReorderLessonsHandler
{
    private readonly ILessonRepository _repository;

    public ReorderLessonsHandler(ILessonRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(ReorderLessonsRequest request, CancellationToken cancellationToken = default)
    {
        var orders = request.Lessons.Select(l => (l.LessonId, l.NewOrder)).ToList();
        await _repository.UpdateOrdersAsync(request.ModuleId, orders);
        return true;
    }
}
