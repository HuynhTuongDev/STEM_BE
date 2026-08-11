using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Classes;
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

    public async Task<Class?> GetClassWithDetailsAsync(
        int classId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Classes
            .Include(classEntity => classEntity.Course)
                .ThenInclude(course => course!.School)
            .Include(classEntity => classEntity.Teacher)
            .Include(classEntity => classEntity.Enrollments)
            .FirstOrDefaultAsync(classEntity => classEntity.Id == classId, cancellationToken);
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
        int? classId,
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

        if (classId.HasValue)
        {
            query = query.Where(template =>
                template.Simulation != null && template.Simulation.ClassId == classId.Value);
        }

        if (courseId.HasValue)
        {
            query = query.Where(template =>
                template.Simulation != null &&
                template.Simulation.Class != null &&
                template.Simulation.Class.CourseId == courseId.Value);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(template =>
                template.Simulation != null &&
                template.Simulation.Class != null &&
                template.Simulation.Class.SchoolId == schoolId.Value);
        }

        if (teacherId.HasValue)
        {
            query = query.Where(template =>
                template.Simulation != null &&
                template.Simulation.Class != null &&
                template.Simulation.Class.TeacherId == teacherId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(template =>
                template.Simulation != null &&
                template.Simulation.Class != null &&
                template.Simulation.Class.Enrollments.Any(enrollment => enrollment.StudentId == studentId.Value));
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

    public async Task<IEnumerable<Enrollment>> GetStudentEnrollmentsAsync(int studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SimulationSession>> GetStudentSubmissionsAsync(int studentId, CancellationToken cancellationToken = default)
    {
        return await _context.SimulationSessions
            .AsNoTracking()
            .Include(s => s.Template)
            .Where(s => s.StudentId == studentId)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<SimulationTemplate> BuildTemplateDetailsQuery()
    {
        return _context.SimulationTemplates
            .Include(template => template.Simulation)
                .ThenInclude(simulation => simulation!.SimulationTemplates)
            .Include(template => template.Simulation)
                .ThenInclude(simulation => simulation!.Class)
                    .ThenInclude(classEntity => classEntity!.Course)
                        .ThenInclude(course => course!.School)
            .Include(template => template.Simulation)
                .ThenInclude(simulation => simulation!.Class)
                    .ThenInclude(classEntity => classEntity!.Teacher)
            .Include(template => template.Simulation)
                .ThenInclude(simulation => simulation!.Class)
                    .ThenInclude(classEntity => classEntity!.Enrollments)
            .Include(template => template.SimulationSessions)
            .AsQueryable();
    }
}
