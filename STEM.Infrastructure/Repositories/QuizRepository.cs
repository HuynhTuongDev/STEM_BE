using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class QuizRepository : Repository<Quiz>, IQuizRepository
{
    public QuizRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Quiz>> GetByCourseIdAsync(
        int courseId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(q => q.Course)
            .Include(q => q.QuizQuestions).ThenInclude(q => q.QuizAnswers)
            .Where(q => q.CourseId == courseId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Quiz?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(q => q.Course).ThenInclude(c => c!.Teacher)
            .Include(q => q.Course).ThenInclude(c => c!.Classes).ThenInclude(c => c.Enrollments)
            .Include(q => q.QuizQuestions).ThenInclude(q => q.QuizAnswers)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Quiz> Quizzes, int TotalCount)> GetStudentQuizzesPagedAsync(
        int studentId,
        int pageNumber,
        int pageSize,
        int? courseId,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(q => q.Course).ThenInclude(c => c!.Teacher)
            .Include(q => q.Course).ThenInclude(c => c!.Classes).ThenInclude(c => c.Enrollments)
            .Include(q => q.QuizQuestions)
            .Where(q => q.Course != null &&
                        q.Course.Classes.Any(c => c.Enrollments.Any(e => e.StudentId == studentId)));

        if (courseId.HasValue)
        {
            query = query.Where(q => q.CourseId == courseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(q =>
                q.Title.ToLower().Contains(term) ||
                (q.Course != null && q.Course.Title.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var quizzes = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (quizzes, totalCount);
    }
}
