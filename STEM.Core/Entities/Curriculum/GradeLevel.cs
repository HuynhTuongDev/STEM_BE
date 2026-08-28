using STEM.Core.Entities.Courses;

namespace STEM.Core.Entities.Curriculum;

public class GradeLevel : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Ví dụ: "Khối 10", "Khối 11", "Khối 12"
    public string Code { get; set; } = string.Empty; // Ví dụ: "GRADE_10", "GRADE_11", "GRADE_12"
    public int DisplayOrder { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    
    // Thứ tự khối (10, 11, 12)
    public int Level { get; set; } = 10;
    
    // Syllabus belong to this grade level
    public ICollection<Syllabus> Syllabi { get; set; } = new List<Syllabus>();
}

public static class GradeLevelCodes
{
    public const string Grade10 = "GRADE_10";
    public const string Grade11 = "GRADE_11";
    public const string Grade12 = "GRADE_12";
    
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Grade10,
        Grade11,
        Grade12
    };
}
