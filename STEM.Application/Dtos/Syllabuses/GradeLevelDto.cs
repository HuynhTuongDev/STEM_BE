namespace STEM.Application.Dtos.Syllabuses;

public class GradeLevelResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int Level { get; set; }
}
