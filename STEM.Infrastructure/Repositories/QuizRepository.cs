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
            .Include(quiz => quiz.Course)
            .Include(quiz => quiz.QuizQuestions)
                .ThenInclude(question => question.QuizAnswers)
            .Where(quiz => quiz.CourseId == courseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Quiz?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(quiz => quiz.Course)
                .ThenInclude(course => course!.Teacher)
            .Include(quiz => quiz.Course)
                .ThenInclude(course => course!.School)
            .Include(quiz => quiz.Course)
                .ThenInclude(course => course!.Classes)
                .ThenInclude(classEntity => classEntity.Enrollments)
            .Include(quiz => quiz.QuizQuestions)
                .ThenInclude(question => question.QuizAnswers)
            .FirstOrDefaultAsync(quiz => quiz.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Quiz> Quizzes, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? courseId,
        int? schoolId,
        int? teacherId,
        int? studentId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(quiz => quiz.Course)
                .ThenInclude(course => course!.Teacher)
            .Include(quiz => quiz.Course)
                .ThenInclude(course => course!.School)
            .Include(quiz => quiz.QuizQuestions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(quiz => quiz.Title.ToLower().Contains(term));
        }

        if (courseId.HasValue)
        {
            query = query.Where(quiz => quiz.CourseId == courseId.Value);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(quiz => quiz.Course != null && quiz.Course.SchoolId == schoolId.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(quiz => quiz.Course != null && quiz.Course.TeacherId == teacherId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(quiz =>
                quiz.Course != null &&
                quiz.Course.Classes.Any(classEntity =>
                    classEntity.Enrollments.Any(enrollment => enrollment.StudentId == studentId.Value)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var quizzes = await query
            .OrderByDescending(quiz => quiz.CreatedAt)
            .ThenBy(quiz => quiz.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (quizzes, totalCount);
    }

    public void DeleteQuestions(IEnumerable<QuizQuestion> questions)
    {
        _context.QuizQuestions.RemoveRange(questions);
    }
}
