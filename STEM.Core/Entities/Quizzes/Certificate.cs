using STEM.Core.Entities.Users;
using STEM.Core.Entities.Courses;

namespace STEM.Core.Entities.Quizzes;

public class Certificate : BaseEntity
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }

    public User? Student { get; set; }
    public Course? Course { get; set; }
}
