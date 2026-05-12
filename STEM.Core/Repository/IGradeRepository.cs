using STEM.Core.Entities.Quizzes;

namespace STEM.Core.Repository;

public interface IGradeRepository : IRepository<Grade>
{
    Task<IEnumerable<Grade>> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);
}