using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Quizzes;

public class QuizAttempt : BaseEntity
{
    public int QuizId { get; set; }
    public int StudentId { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public decimal Score { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Quiz? Quiz { get; set; }
    public User? Student { get; set; }
    public ICollection<QuizAttemptAnswer> Answers { get; set; } = new List<QuizAttemptAnswer>();
}
