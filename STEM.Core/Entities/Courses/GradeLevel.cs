namespace STEM.Core.Entities.Courses;

public class GradeLevel : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Level { get; set; }

    public ICollection<Syllabus> Syllabuses { get; set; } = new List<Syllabus>();
}
