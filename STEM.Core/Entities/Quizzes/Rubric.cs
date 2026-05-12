using STEM.Core.Entities.Common;

namespace STEM.Core.Entities.Quizzes;

public class Rubric : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Criteria { get; set; } = string.Empty; // JSON structure for criteria

    public ICollection<Grade> Grades { get; set; } = [];
}
