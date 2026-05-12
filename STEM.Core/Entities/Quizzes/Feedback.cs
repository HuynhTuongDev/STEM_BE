using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Quizzes;

public class Feedback : BaseEntity
{
    public int StudentId { get; set; }
    public string Content { get; set; } = string.Empty;

    public User? Student { get; set; }
}
