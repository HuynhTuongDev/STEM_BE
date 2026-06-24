using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class QuizAttemptRepository : Repository<QuizAttempt>, IQuizAttemptRepository
{
    public QuizAttemptRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<QuizAttempt?> GetLatestByQuizAndStudentAsync(
        int quizId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Quiz).ThenInclude(q => q!.Course)
            .Include(a => a.Answers).ThenInclude(a => a.Question).ThenInclude(q => q!.QuizAnswers)
            .Include(a => a.Answers).ThenInclude(a => a.Answer)
            .Where(a => a.QuizId == quizId && a.StudentId == studentId)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<QuizAttempt?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Quiz).ThenInclude(q => q!.Course)
            .Include(a => a.Student)
            .Include(a => a.Answers).ThenInclude(a => a.Question).ThenInclude(q => q!.QuizAnswers)
            .Include(a => a.Answers).ThenInclude(a => a.Answer)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}
