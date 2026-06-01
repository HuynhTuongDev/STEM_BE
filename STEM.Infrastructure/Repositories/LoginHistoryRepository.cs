using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class LoginHistoryRepository : Repository<LoginHistory>, ILoginHistoryRepository
{
    public LoginHistoryRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<LoginHistory>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lh => lh.UserId == userId)
            .OrderByDescending(lh => lh.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LoginHistory>> GetByUserIdAndSchoolIdAsync(int userId, int schoolId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(lh => lh.User)
            .Where(lh => lh.UserId == userId && lh.User != null && lh.User.SchoolId == schoolId)
            .OrderByDescending(lh => lh.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
