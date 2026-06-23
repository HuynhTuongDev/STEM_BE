using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Users;
using STEM.Core.Entities.Schools;

namespace STEM.Core.Entities.Classes;

public class Class : BaseEntity
{
    public string ClassCode { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public int CourseId { get; set; }
    public int TeacherId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public School? School { get; set; }
    public Course? Course { get; set; }
    public User? Teacher { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
}
