using STEM.Core.Entities.Quizzes;

namespace STEM.Core.Repository;

public interface IQuizRepository : IRepository<Quiz>
{
    Task<IEnumerable<Quiz>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default);
}