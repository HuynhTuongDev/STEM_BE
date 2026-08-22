using STEM.Application.Dtos.Curriculum;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Curriculum;

public class GetSyllabiHandler
{
    private readonly ISyllabusRepository _repository;

    public GetSyllabiHandler(ISyllabusRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<SyllabusDto>> Handle(string? status = null, int? gradeLevelId = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Syllabus> syllabi;

        if (!string.IsNullOrWhiteSpace(status))
        {
            syllabi = await _repository.GetByStatusAsync(status);
        }
        else if (gradeLevelId.HasValue)
        {
            syllabi = await _repository.GetByGradeLevelWithDetailsAsync(gradeLevelId.Value);
        }
        else
        {
            syllabi = await _repository.GetAllWithDetailsAsync();
        }

        var result = new List<SyllabusDto>();
        foreach (var s in syllabi)
        {
            result.Add(new SyllabusDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                ThumbnailUrl = s.ThumbnailUrl,
                GradeLevelId = s.GradeLevelId,
                GradeLevelName = s.GradeLevel?.Name,
                SubjectArea = s.SubjectArea,
                Status = s.Status,
                DisplayOrder = s.DisplayOrder,
                EstimatedHours = s.EstimatedHours,
                IsRequired = s.IsRequired,
                IsSystemOwned = s.IsSystemOwned,
                CourseCount = await _repository.GetCourseCountAsync(s.Id),
                TotalModules = await _repository.GetTotalModulesAsync(s.Id),
                TotalLessons = await _repository.GetTotalLessonsAsync(s.Id),
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            });
        }

        return result;
    }
}

public class GetSyllabusByIdHandler
{
    private readonly ISyllabusRepository _repository;

    public GetSyllabusByIdHandler(ISyllabusRepository repository)
    {
        _repository = repository;
    }

    public async Task<SyllabusDetailDto?> Handle(int id, CancellationToken cancellationToken = default)
    {
        var syllabus = await _repository.GetByIdWithDetailsAsync(id);
        if (syllabus == null)
            return null;

        var dto = new SyllabusDetailDto
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
            CourseCount = syllabus.Courses.Count,
            TotalModules = syllabus.Courses.SelectMany(c => c.Modules).Count(),
            TotalLessons = syllabus.Courses.SelectMany(c => c.Modules).SelectMany(m => m.Lessons).Count(),
            CreatedAt = syllabus.CreatedAt,
            UpdatedAt = syllabus.UpdatedAt,
            Courses = syllabus.Courses.OrderBy(c => c.DisplayOrder).Select(c => new CourseInSyllabusDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                EstimatedHours = c.EstimatedHours,
                IsRequired = c.IsRequired,
                Status = c.Status,
                Modules = c.Modules.OrderBy(m => m.DisplayOrder).Select(m => new ModuleInCourseDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    DisplayOrder = m.DisplayOrder,
                    EstimatedMinutes = m.EstimatedMinutes,
                    Input = m.Input,
                    Output = m.Output,
                    Lessons = m.Lessons.OrderBy(l => l.DisplayOrder).Select(l => new LessonInModuleDto
                    {
                        Id = l.Id,
                        Title = l.Title,
                        DisplayOrder = l.DisplayOrder,
                        EstimatedMinutes = l.EstimatedMinutes,
                        LessonType = l.LessonType,
                        Input = l.Input,
                        Output = l.Output,
                        HasVirtualLab = l.HasVirtualLab,
                        LabId = l.LabId
                    }).ToList()
                }).ToList()
            }).ToList()
        };

        return dto;
    }
}

public class CreateSyllabusHandler
{
    private readonly ISyllabusRepository _repository;
    private readonly IGradeLevelRepository _gradeLevelRepository;

    public CreateSyllabusHandler(ISyllabusRepository repository, IGradeLevelRepository gradeLevelRepository)
    {
        _repository = repository;
        _gradeLevelRepository = gradeLevelRepository;
    }

    public async Task<int> Handle(CreateSyllabusRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        if (request.GradeLevelId.HasValue)
        {
            var gradeLevelExists = await _gradeLevelRepository.ExistsAsync(request.GradeLevelId.Value);
            if (!gradeLevelExists)
                throw new InvalidOperationException($"Grade level with ID {request.GradeLevelId} does not exist.");
        }

        var syllabus = new Syllabus
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            ThumbnailUrl = request.ThumbnailUrl,
            GradeLevelId = request.GradeLevelId,
            SubjectArea = request.SubjectArea,
            Status = SyllabusStatuses.Draft,
            DisplayOrder = request.DisplayOrder,
            EstimatedHours = request.EstimatedHours,
            IsRequired = request.IsRequired,
            IsSystemOwned = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(syllabus);
        return syllabus.Id;
    }
}

public class UpdateSyllabusHandler
{
    private readonly ISyllabusRepository _repository;
    private readonly IGradeLevelRepository _gradeLevelRepository;

    public UpdateSyllabusHandler(ISyllabusRepository repository, IGradeLevelRepository gradeLevelRepository)
    {
        _repository = repository;
        _gradeLevelRepository = gradeLevelRepository;
    }

    public async Task<bool> Handle(int id, UpdateSyllabusRequest request, CancellationToken cancellationToken = default)
    {
        var syllabus = await _repository.GetByIdAsync(id);
        if (syllabus == null)
            return false;

        if (syllabus.Status != SyllabusStatuses.Draft)
            throw new InvalidOperationException("Only Draft syllabi can be edited.");

        if (request.GradeLevelId.HasValue)
        {
            var gradeLevelExists = await _gradeLevelRepository.ExistsAsync(request.GradeLevelId.Value);
            if (!gradeLevelExists)
                throw new InvalidOperationException($"Grade level with ID {request.GradeLevelId} does not exist.");
        }

        syllabus.Title = request.Title.Trim();
        syllabus.Description = request.Description?.Trim() ?? string.Empty;
        syllabus.ThumbnailUrl = request.ThumbnailUrl;
        syllabus.GradeLevelId = request.GradeLevelId;
        syllabus.SubjectArea = request.SubjectArea;
        syllabus.DisplayOrder = request.DisplayOrder;
        syllabus.EstimatedHours = request.EstimatedHours;
        syllabus.IsRequired = request.IsRequired;
        syllabus.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(syllabus);
        return true;
    }
}

public class DeleteSyllabusHandler
{
    private readonly ISyllabusRepository _repository;

    public DeleteSyllabusHandler(ISyllabusRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(int id, CancellationToken cancellationToken = default)
    {
        var syllabus = await _repository.GetByIdAsync(id);
        if (syllabus == null)
            return false;

        if (syllabus.Status != SyllabusStatuses.Draft)
            throw new InvalidOperationException("Only Draft syllabi can be deleted.");

        return await _repository.DeleteAsync(id);
    }
}

public class PublishSyllabusHandler
{
    private readonly ISyllabusRepository _repository;

    public PublishSyllabusHandler(ISyllabusRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(int id, CancellationToken cancellationToken = default)
    {
        var syllabus = await _repository.GetByIdAsync(id);
        if (syllabus == null)
            return false;

        if (syllabus.Status != SyllabusStatuses.Draft)
            throw new InvalidOperationException("Only Draft syllabi can be published.");

        return await _repository.PublishAsync(id);
    }
}

public class ArchiveSyllabusHandler
{
    private readonly ISyllabusRepository _repository;

    public ArchiveSyllabusHandler(ISyllabusRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(int id, CancellationToken cancellationToken = default)
    {
        var syllabus = await _repository.GetByIdAsync(id);
        if (syllabus == null)
            return false;

        if (syllabus.Status != SyllabusStatuses.Published)
            throw new InvalidOperationException("Only Published syllabi can be archived.");

        return await _repository.ArchiveAsync(id);
    }
}

public class UnpublishSyllabusHandler
{
    private readonly ISyllabusRepository _repository;

    public UnpublishSyllabusHandler(ISyllabusRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(int id, CancellationToken cancellationToken = default)
    {
        var syllabus = await _repository.GetByIdAsync(id);
        if (syllabus == null)
            return false;

        if (syllabus.Status != SyllabusStatuses.Published)
            throw new InvalidOperationException("Only Published syllabi can be unpublished.");

        syllabus.Status = SyllabusStatuses.Draft;
        syllabus.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(syllabus);
        return true;
    }
}

public class RestoreSyllabusHandler
{
    private readonly ISyllabusRepository _repository;

    public RestoreSyllabusHandler(ISyllabusRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(int id, CancellationToken cancellationToken = default)
    {
        var syllabus = await _repository.GetByIdAsync(id);
        if (syllabus == null)
            return false;

        if (syllabus.Status != SyllabusStatuses.Archived)
            throw new InvalidOperationException("Only Archived syllabi can be restored.");

        syllabus.Status = SyllabusStatuses.Draft;
        syllabus.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(syllabus);
        return true;
    }
}
