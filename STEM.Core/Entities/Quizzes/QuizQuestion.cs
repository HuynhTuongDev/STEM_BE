namespace STEM.Core.Entities.Quizzes;

public class QuizQuestion : BaseEntity
{
    public int QuizId { get; set; }
    public string Content { get; set; } = string.Empty;

    public Quiz? Quiz { get; set; }
    public ICollection<QuizAnswer> QuizAnswers { get; set; } = new List<QuizAnswer>();
}
