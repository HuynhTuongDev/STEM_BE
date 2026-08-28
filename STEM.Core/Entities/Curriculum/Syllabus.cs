using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Curriculum;

public class Syllabus : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    
    // Cấp độ học (Khối 10, 11, 12)
    public int? GradeLevelId { get; set; }
    
    // Lĩnh vực STEM: Engineering là lĩnh vực nghiên cứu chính
    public string SubjectArea { get; set; } = SubjectAreas.Engineering;
    
    // Trạng thái: Draft, Published, Archived
    public string Status { get; set; } = SyllabusStatuses.Draft;
    
    // Thứ tự hiển thị trong khối
    public int DisplayOrder { get; set; } = 0;
    
    // Thời lượng ước tính (giờ)
    public int EstimatedHours { get; set; } = 0;
    
    // Có bắt buộc không
    public bool IsRequired { get; set; } = true;
    
    // Có phải của hệ thống (Master Admin tạo) không
    public bool IsSystemOwned { get; set; } = true;
    
    // Reference
    public GradeLevel? GradeLevel { get; set; }
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}

public static class SyllabusStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";
    
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Draft,
        Published,
        Archived
    };
}

public static class SubjectAreas
{
    // Theo cấu trúc STEM - Engineering là lĩnh vực nghiên cứu chính
    public const string Science = "science";      // Khoa học tự nhiên (Vật lý, Hóa, Sinh)
    public const string Technology = "technology"; // Công nghệ
    public const string Engineering = "engineering"; // Kỹ thuật (Lĩnh vực nghiên cứu chính - Virtual Lab)
    public const string Mathematics = "mathematics"; // Toán
    
    // Các môn cụ thể trong STEM
    public const string Physics = "physics";
    public const string Chemistry = "chemistry";
    public const string Biology = "biology";
    
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Science,
        Technology,
        Engineering,
        Mathematics,
        Physics,
        Chemistry,
        Biology
    };
}
