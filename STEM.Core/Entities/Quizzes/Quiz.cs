using STEM.Core.Entities.Courses;

namespace STEM.Core.Entities.Quizzes;

public class Quiz : BaseEntity
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;

    public Course? Course { get; set; }
    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
}
