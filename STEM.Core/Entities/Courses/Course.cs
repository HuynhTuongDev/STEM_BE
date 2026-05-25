using STEM.Core.Entities.Users;
using STEM.Core.Entities.Schools;

namespace STEM.Core.Entities.Courses;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public int? SchoolId { get; set; }

    public User? Teacher { get; set; }
    public School? School { get; set; }
}
