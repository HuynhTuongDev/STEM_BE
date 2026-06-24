using STEM.Core.Entities.Quizzes;

namespace STEM.Core.Repository;

public interface IQuizAttemptRepository : IRepository<QuizAttempt>
{
    Task<QuizAttempt?> GetLatestByQuizAndStudentAsync(
        int quizId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<QuizAttempt?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
}
