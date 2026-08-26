namespace STEM.Application.Dtos.Curriculum;

public class GradeLevelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int SyllabusCount { get; set; }
    public int CourseCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateGradeLevelRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
}

public class UpdateGradeLevelRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
