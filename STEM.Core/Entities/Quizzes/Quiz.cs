using STEM.Core.Entities.Classes;

namespace STEM.Core.Entities.Quizzes;

public class Quiz : BaseEntity
{
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;

    public Class? Class { get; set; }
    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
}
