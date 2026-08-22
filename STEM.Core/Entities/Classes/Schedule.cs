using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Classes;

public class Schedule : BaseEntity
{
    public int ClassId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? LessonId { get; set; }

    // Navigation properties
    public Class? Class { get; set; }
    public Lesson? Lesson { get; set; }
}
