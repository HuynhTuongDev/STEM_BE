using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        // Assuming email is used as username or add actual username property to User
        return await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(u => u.FullName == username, cancellationToken);
    }
}
