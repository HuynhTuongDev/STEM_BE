using STEM.Application.Dtos.Curriculum;
using STEM.Core.Entities.Curriculum;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Curriculum;

public class GetGradeLevelsHandler
{
    private readonly IGradeLevelRepository _repository;

    public GetGradeLevelsHandler(IGradeLevelRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<GradeLevelDto>> Handle(CancellationToken cancellationToken = default)
    {
        var gradeLevels = await _repository.GetAllOrderedAsync();
        
        var result = new List<GradeLevelDto>();
        foreach (var gl in gradeLevels)
        {
            result.Add(new GradeLevelDto
            {
                Id = gl.Id,
                Name = gl.Name,
                Code = gl.Code,
                Level = gl.Level,
                Description = gl.Description,
                DisplayOrder = gl.DisplayOrder,
                SyllabusCount = await _repository.GetSyllabusCountAsync(gl.Id),
                CreatedAt = gl.CreatedAt,
                UpdatedAt = gl.UpdatedAt
            });
        }

        return result;
    }
}

public class GetGradeLevelByIdHandler
{
    private readonly IGradeLevelRepository _repository;

    public GetGradeLevelByIdHandler(IGradeLevelRepository repository)
    {
        _repository = repository;
    }

    public async Task<GradeLevelDto?> Handle(int id, CancellationToken cancellationToken = default)
    {
        var gradeLevel = await _repository.GetByIdAsync(id);
        if (gradeLevel == null)
            return null;

        return new GradeLevelDto
        {
            Id = gradeLevel.Id,
            Name = gradeLevel.Name,
            Code = gradeLevel.Code,
            Level = gradeLevel.Level,
            Description = gradeLevel.Description,
            DisplayOrder = gradeLevel.DisplayOrder,
            SyllabusCount = await _repository.GetSyllabusCountAsync(id),
            CreatedAt = gradeLevel.CreatedAt,
            UpdatedAt = gradeLevel.UpdatedAt
        };
    }
}

public class CreateGradeLevelHandler
{
    private readonly IGradeLevelRepository _repository;

    public CreateGradeLevelHandler(IGradeLevelRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> Handle(CreateGradeLevelRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Code is required.");

        if (await _repository.ExistsByCodeAsync(request.Code))
            throw new InvalidOperationException($"A grade level with code '{request.Code}' already exists.");

        var gradeLevel = new GradeLevel
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpper(),
            Level = request.Level,
            Description = request.Description?.Trim() ?? string.Empty,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(gradeLevel);
        return gradeLevel.Id;
    }
}

public class UpdateGradeLevelHandler
{
    private readonly IGradeLevelRepository _repository;

    public UpdateGradeLevelHandler(IGradeLevelRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(int id, UpdateGradeLevelRequest request, CancellationToken cancellationToken = default)
    {
        var gradeLevel = await _repository.GetByIdAsync(id);
        if (gradeLevel == null)
            return false;

        if (await _repository.ExistsByCodeAsync(request.Code, id))
            throw new InvalidOperationException($"A grade level with code '{request.Code}' already exists.");

        gradeLevel.Name = request.Name.Trim();
        gradeLevel.Code = request.Code.Trim().ToUpper();
        gradeLevel.Level = request.Level;
        gradeLevel.Description = request.Description?.Trim() ?? string.Empty;
        gradeLevel.DisplayOrder = request.DisplayOrder;
        gradeLevel.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(gradeLevel);
        return true;
    }
}

public class DeleteGradeLevelHandler
{
    private readonly IGradeLevelRepository _repository;

    public DeleteGradeLevelHandler(IGradeLevelRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(int id, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteAsync(id);
    }
}
