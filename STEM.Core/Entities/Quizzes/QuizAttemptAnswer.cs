namespace STEM.Core.Entities.Quizzes;

public class QuizAttemptAnswer : BaseEntity
{
    public int QuizAttemptId { get; set; }
    public int QuestionId { get; set; }
    public int? AnswerId { get; set; }
    public bool IsCorrect { get; set; }

    public QuizAttempt? QuizAttempt { get; set; }
    public QuizQuestion? Question { get; set; }
    public QuizAnswer? Answer { get; set; }
}
