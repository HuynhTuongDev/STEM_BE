using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Quizzes;

public class Leaderboard : BaseEntity
{
    public int StudentId { get; set; }
    public int Score { get; set; }

    public User? Student { get; set; }
}
