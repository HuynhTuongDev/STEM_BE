using STEM.Core.Entities.Quizzes;

namespace STEM.Core.Repository;

public interface IQuizRepository : IRepository<Quiz>
{
    Task<IEnumerable<Quiz>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default);

    Task<Quiz?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<(IEnumerable<Quiz> Quizzes, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? courseId,
        int? schoolId,
        int? teacherId,
        int? studentId,
        CancellationToken cancellationToken = default);

    void DeleteQuestions(IEnumerable<QuizQuestion> questions);
}
