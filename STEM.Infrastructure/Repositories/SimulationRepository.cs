using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Simulations;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class SimulationRepository : Repository<SimulationTemplate>, ISimulationRepository
{
    public SimulationRepository(StemDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SimulationTemplate>> GetByStudentAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SimulationTemplates
            .Include(t => t.SimulationSessions)
            .Where(t => t.SimulationSessions.Any(s => s.StudentId == studentId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Lesson?> GetLessonWithDetailsAsync(
        int lessonId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Lessons
            .Include(lesson => lesson.Module)
                .ThenInclude(module => module!.Course)
                    .ThenInclude(course => course!.Teacher)
            .Include(lesson => lesson.Module)
                .ThenInclude(module => module!.Course)
                    .ThenInclude(course => course!.School)
            .Include(lesson => lesson.Module)
                .ThenInclude(module => module!.Course)
                    .ThenInclude(course => course!.Classes)
                        .ThenInclude(classEntity => classEntity.Enrollments)
            .FirstOrDefaultAsync(lesson => lesson.Id == lessonId, cancellationToken);
    }

    public async Task<SimulationTemplate?> GetTemplateWithDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await BuildTemplateDetailsQuery()
            .FirstOrDefaultAsync(template => template.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<SimulationTemplate> Templates, int TotalCount)> GetTemplatesPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? lessonId,
        int? courseId,
        int? schoolId,
        int? teacherId,
        int? studentId,
        CancellationToken cancellationToken = default)
    {
        var query = BuildTemplateDetailsQuery();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(template =>
                template.SimulationName.ToLower().Contains(term) ||
                template.Description.ToLower().Contains(term));
        }

        if (lessonId.HasValue)
        {
            query = query.Where(template =>
                template.Simulation != null && template.Simulation.LessonId == lessonId.Value);
        }

        if (courseId.HasValue)
        {
            query = query.Where(template =>
                template.Simulation != null &&
                template.Simulation.Lesson != null &&
                template.Simulation.Lesson.Module != null &&
                template.Simulation.Lesson.Module.CourseId == courseId.Value);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(template =>
                template.Simulation != null &&
                template.Simulation.Lesson != null &&
                template.Simulation.Lesson.Module != null &&
                template.Simulation.Lesson.Module.Course != null &&
                template.Simulation.Lesson.Module.Course.SchoolId == schoolId.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(template =>
                template.Simulation != null &&
                template.Simulation.Lesson != null &&
                template.Simulation.Lesson.Module != null &&
                template.Simulation.Lesson.Module.Course != null &&
                template.Simulation.Lesson.Module.Course.TeacherId == teacherId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(template =>
                template.Simulation != null &&
                template.Simulation.Lesson != null &&
                template.Simulation.Lesson.Module != null &&
                template.Simulation.Lesson.Module.Course != null &&
                template.Simulation.Lesson.Module.Course.Classes.Any(classEntity =>
                    classEntity.Enrollments.Any(enrollment => enrollment.StudentId == studentId.Value)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var templates = await query
            .OrderByDescending(template => template.CreatedAt)
            .ThenBy(template => template.SimulationName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (templates, totalCount);
    }

    public async Task AddSimulationAsync(
        SimulationEntity simulation,
        CancellationToken cancellationToken = default)
    {
        await _context.Simulations.AddAsync(simulation, cancellationToken);
    }

    public void DeleteSimulation(SimulationEntity simulation)
    {
        _context.Simulations.Remove(simulation);
    }

    private IQueryable<SimulationTemplate> BuildTemplateDetailsQuery()
    {
        return _context.SimulationTemplates
            .Include(template => template.Simulation)
                .ThenInclude(simulation => simulation!.SimulationTemplates)
            .Include(template => template.Simulation)
                .ThenInclude(simulation => simulation!.Lesson)
                    .ThenInclude(lesson => lesson!.Module)
                        .ThenInclude(module => module!.Course)
                            .ThenInclude(course => course!.Teacher)
            .Include(template => template.Simulation)
                .ThenInclude(simulation => simulation!.Lesson)
                    .ThenInclude(lesson => lesson!.Module)
                        .ThenInclude(module => module!.Course)
                            .ThenInclude(course => course!.School)
            .Include(template => template.Simulation)
                .ThenInclude(simulation => simulation!.Lesson)
                    .ThenInclude(lesson => lesson!.Module)
                        .ThenInclude(module => module!.Course)
                            .ThenInclude(course => course!.Classes)
                                .ThenInclude(classEntity => classEntity.Enrollments)
            .Include(template => template.SimulationSessions)
            .AsQueryable();
    }
}
