namespace STEM.Core.Entities.Quizzes;

public class QuizAnswer : BaseEntity
{
    public int QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    public QuizQuestion? Question { get; set; }
}
