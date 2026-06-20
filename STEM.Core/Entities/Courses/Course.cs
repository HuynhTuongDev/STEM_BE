using STEM.Core.Entities.Users;
using STEM.Core.Entities.Schools;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Quizzes;

namespace STEM.Core.Entities.Courses;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public int? SchoolId { get; set; }

    public User? Teacher { get; set; }
    public School? School { get; set; }
    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<Module> Modules { get; set; } = new List<Module>();
    public ICollection<Material> Materials { get; set; } = new List<Material>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
