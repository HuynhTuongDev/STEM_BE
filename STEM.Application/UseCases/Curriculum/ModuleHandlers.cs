using STEM.Application.Dtos.Curriculum;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Curriculum;

public class GetModulesHandler
{
    private readonly IModuleRepository _repository;

    public GetModulesHandler(IModuleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ModuleDto>> Handle(int courseId, CancellationToken cancellationToken = default)
    {
        var modules = await _repository.GetByCourseIdOrderedAsync(courseId);
        
        var result = new List<ModuleDto>();
        foreach (var m in modules)
        {
            result.Add(new ModuleDto
            {
                Id = m.Id,
                CourseId = m.CourseId,
                Title = m.Title,
                Description = m.Description,
                DisplayOrder = m.DisplayOrder,
                EstimatedMinutes = m.EstimatedMinutes,
                Input = m.Input,
                Output = m.Output,
                LessonCount = await _repository.GetLessonCountAsync(m.Id),
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            });
        }

        return result;
    }
}

public class GetModuleByIdHandler
{
    private readonly IModuleRepository _repository;

    public GetModuleByIdHandler(IModuleRepository repository)
    {
        _repository = repository;
    }

    public async Task<ModuleDetailDto?> Handle(int id, CancellationToken cancellationToken = default)
    {
        var module = await _repository.GetByIdWithLessonsAsync(id);
        if (module == null)
            return null;

        return new ModuleDetailDto
        {
            Id = module.Id,
            CourseId = module.CourseId,
            Title = module.Title,
            Description = module.Description,
            DisplayOrder = module.DisplayOrder,
            EstimatedMinutes = module.EstimatedMinutes,
            Input = module.Input,
            Output = module.Output,
            LessonCount = module.Lessons.Count,
            CreatedAt = module.CreatedAt,
            UpdatedAt = module.UpdatedAt,
            Lessons = module.Lessons.OrderBy(l => l.DisplayOrder).Select(l => new LessonInModuleDto
            {
                Id = l.Id,
                Title = l.Title,
                Description = l.Description,
                DisplayOrder = l.DisplayOrder,
                EstimatedMinutes = l.EstimatedMinutes,
                LessonType = l.LessonType,
                HasVirtualLab = l.HasVirtualLab,
                LabId = l.LabId
            }).ToList()
        };
    }
}

public class CreateModuleHandler
{
    private readonly IModuleRepository _repository;
    private readonly ICourseRepository _courseRepository;

    public CreateModuleHandler(IModuleRepository repository, ICourseRepository courseRepository)
    {
        _repository = repository;
        _courseRepository = courseRepository;
    }

    public async Task<int> Handle(CreateModuleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var course = await _courseRepository.GetByIdAsync(request.CourseId);
        if (course == null)
            throw new InvalidOperationException($"Course with ID {request.CourseId} does not exist.");

        if (course.Status != SyllabusStatuses.Draft)
            throw new InvalidOperationException("Only Draft courses can have modules added.");

        var module = new Module
        {
            CourseId = request.CourseId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Input = request.Input?.Trim() ?? string.Empty,
            Output = request.Output?.Trim() ?? string.Empty,
            DisplayOrder = request.DisplayOrder,
            EstimatedMinutes = request.EstimatedMinutes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(module);
        return module.Id;
    }
}

public class UpdateModuleHandler
{
    private readonly IModuleRepository _repository;
    private readonly ICourseRepository _courseRepository;

    public UpdateModuleHandler(IModuleRepository repository, ICourseRepository courseRepository)
    {
        _repository = repository;
        _courseRepository = courseRepository;
    }

    public async Task<bool> Handle(int id, UpdateModuleRequest request, CancellationToken cancellationToken = default)
    {
        var module = await _repository.GetByIdAsync(id);
        if (module == null)
            return false;

        var course = await _courseRepository.GetByIdAsync(module.CourseId);
        if (course != null && course.Status != SyllabusStatuses.Draft)
            throw new InvalidOperationException("Only modules in Draft courses can be edited.");

        module.Title = request.Title.Trim();
        module.Description = request.Description?.Trim() ?? string.Empty;
        module.Input = request.Input?.Trim() ?? string.Empty;
        module.Output = request.Output?.Trim() ?? string.Empty;
        module.DisplayOrder = request.DisplayOrder;
        module.EstimatedMinutes = request.EstimatedMinutes;
        module.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(module);
        return true;
    }
}

public class DeleteModuleHandler
{
    private readonly IModuleRepository _repository;
    private readonly ICourseRepository _courseRepository;

    public DeleteModuleHandler(IModuleRepository repository, ICourseRepository courseRepository)
    {
        _repository = repository;
        _courseRepository = courseRepository;
    }

    public async Task<bool> Handle(int id, CancellationToken cancellationToken = default)
    {
        var module = await _repository.GetByIdAsync(id);
        if (module == null)
            return false;

        var course = await _courseRepository.GetByIdAsync(module.CourseId);
        if (course != null && course.Status != SyllabusStatuses.Draft)
            throw new InvalidOperationException("Only modules in Draft courses can be deleted.");

        return await _repository.DeleteAsync(id);
    }
}

public class ReorderModulesHandler
{
    private readonly IModuleRepository _repository;

    public ReorderModulesHandler(IModuleRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(ReorderModulesRequest request, CancellationToken cancellationToken = default)
    {
        var orders = request.Modules.Select(m => (m.ModuleId, m.NewOrder)).ToList();
        await _repository.UpdateOrdersAsync(request.CourseId, orders);
        return true;
    }
}

public class GetModulesByClassHandler
{
    private readonly IModuleRepository _moduleRepository;
    private readonly IClassRepository _classRepository;

    public GetModulesByClassHandler(IModuleRepository moduleRepository, IClassRepository classRepository)
    {
        _moduleRepository = moduleRepository;
        _classRepository = classRepository;
    }

    public async Task<IEnumerable<ModuleWithClassDto>> Handle(int classId, CancellationToken cancellationToken = default)
    {
        var classEntity = await _classRepository.GetByIdAsync(classId);
        if (classEntity == null)
            throw new KeyNotFoundException($"Class with ID {classId} not found.");

        var modules = await _moduleRepository.GetByCourseIdOrderedAsync(classEntity.CourseId);

        var result = new List<ModuleWithClassDto>();
        foreach (var m in modules)
        {
            result.Add(new ModuleWithClassDto
            {
                Id = m.Id,
                CourseId = m.CourseId,
                Title = m.Title,
                Description = m.Description,
                DisplayOrder = m.DisplayOrder,
                EstimatedMinutes = m.EstimatedMinutes,
                Input = m.Input,
                Output = m.Output,
                LessonCount = await _moduleRepository.GetLessonCountAsync(m.Id),
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                ClassId = classId,
                ClassName = classEntity.ClassCode
            });
        }

        return result;
    }
}
