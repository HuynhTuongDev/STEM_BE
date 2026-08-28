using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Payments;
using STEM.Core.Interfaces;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class TokenAllocationRepository : Repository<TokenAllocation>, ITokenAllocationRepository
{
    public TokenAllocationRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TokenAllocation>> GetByAccountIdAsync(int accountId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.User)
            .Include(a => a.AllocatedByUser)
            .Where(a => a.AccountId == accountId && a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TokenAllocation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Account)
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TokenAllocation?> GetActiveAllocationAsync(int accountId, int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AccountId == accountId && a.UserId == userId && a.IsActive, cancellationToken);
    }

    public async Task<int> GetCountByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(a => a.AccountId == accountId && a.IsActive, cancellationToken);
    }

    public async Task<int> GetTotalAllocatedByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var allocations = await _dbSet
            .Where(a => a.AccountId == accountId && a.IsActive)
            .ToListAsync(cancellationToken);

        return allocations.Sum(a => a.AllocatedTokens - a.UsedTokens);
    }

    public async Task<TokenAllocation?> GetByIdWithUserAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.User)
            .Include(a => a.AllocatedByUser)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TokenAllocation>> GetExpiredAllocationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(a => a.Account)
            .Include(a => a.User)
            .Where(a => a.IsActive 
                && a.ExpiresAt.HasValue 
                && a.ExpiresAt.Value < now
                && a.AllocatedTokens > a.UsedTokens) // Còn token chưa dùng
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TokenAllocation>> GetExpiredByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(a => a.User)
            .Where(a => a.AccountId == accountId 
                && a.IsActive 
                && a.ExpiresAt.HasValue 
                && a.ExpiresAt.Value < now
                && a.AllocatedTokens > a.UsedTokens)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int TeacherCount, int StudentCount, int TeacherTokens, int StudentTokens)> GetAllocationStatsByRoleAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var allocations = await _dbSet
            .Include(a => a.User)
            .ThenInclude(u => u!.Role)
            .Where(a => a.AccountId == accountId && a.IsActive)
            .ToListAsync(cancellationToken);

        var teacherAllocations = allocations.Where(a => a.User?.RoleId == 3).ToList(); // RoleId 3 = Teacher
        var studentAllocations = allocations.Where(a => a.User?.RoleId == 4).ToList(); // RoleId 4 = Student

        return (
            TeacherCount: teacherAllocations.Count,
            StudentCount: studentAllocations.Count,
            TeacherTokens: teacherAllocations.Sum(a => a.AllocatedTokens - a.UsedTokens),
            StudentTokens: studentAllocations.Sum(a => a.AllocatedTokens - a.UsedTokens)
        );
    }
}
