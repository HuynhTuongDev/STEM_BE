using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Schools;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class SchoolRepository : Repository<School>, ISchoolRepository
{
    public SchoolRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsAsync(int? schoolId, CancellationToken cancellationToken = default)
    {
        if (!schoolId.HasValue)
            return false;

        return await _dbSet.AnyAsync(s => s.Id == schoolId.Value, cancellationToken);
    }
}
