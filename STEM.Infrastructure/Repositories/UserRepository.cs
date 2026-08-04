using Microsoft.EntityFrameworkCore;
using STEM.Application.Dtos.Users;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(StemDbContext context) : base(context)
    {
    }

    public override async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.FullName == username, cancellationToken);
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? roleId,
        bool? isActive,
        int? schoolId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .AsQueryable();

        if (roleId.HasValue)
        {
            query = query.Where(u => u.RoleId == roleId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (schoolId.HasValue)
        {
            query = query.Where(u => u.SchoolId == schoolId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) 
                                     || u.Email.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    public async Task<IEnumerable<User>> GetStudentsNotInClassAsync(int classId, int schoolId, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var studentRoleName = RoleNames.Student;
        var query = _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .Where(u => !_context.Enrollments.Any(e => e.ClassId == classId && e.StudentId == u.Id))
            .Where(u => u.SchoolId == schoolId)
            .Where(u => u.Role != null && u.Role.Name == studentRoleName)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term) 
                                     || u.Email.ToLower().Contains(term));
        }

        return await query
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Schedule>> GetStudentSchedulesAsync(int studentId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.Enrollments
            .Include(e => e.Class)
                .ThenInclude(c => c.Course)
            .Include(e => e.Class.Schedules)
            .Where(e => e.StudentId == studentId)
            .SelectMany(e => e.Class.Schedules)
            .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(s => s.StartTime >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.EndTime <= toDate.Value);

        return await query
            .Include(s => s.Class)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetTeachersWithClassCountAsync(int schoolId, int page, int pageSize, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var teacherRoleId = 3; // Teacher

        var query = _dbSet
            .Include(u => u.Role)
            .Include(u => u.School)
            .Where(u => u.SchoolId == schoolId && u.RoleId == teacherRoleId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(term)
                                     || u.Email.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }
}
