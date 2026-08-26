namespace STEM.Core.Entities.Courses;

public class Module : BaseEntity
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // === INPUT & OUTPUT for Engineering Modules (Chương) ===
    // Input: Những kiến thức/tiên quyết cần có TRƯỚC KHI học chương này
    // Ví dụ: "HS đã học về điện trở, tụ điện, hiểu định luật Ohm"
    public string Input { get; set; } = string.Empty;
    
    // Output: Những gì học sinh SẼ ĐẠT ĐƯỢC SAU KHI học xong chương này
    // Ví dụ: "HS có thể phân tích, thiết kế và xây dựng các mạch điện tử cơ bản"
    public string Output { get; set; } = string.Empty;
    // =======================================================
    
    // Display order within the course
    public int DisplayOrder { get; set; } = 0;
    
    // Estimated duration in minutes (tổng thời gian tất cả bài trong chương)
    public int EstimatedMinutes { get; set; } = 0;
    
    // References
    public Course? Course { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
