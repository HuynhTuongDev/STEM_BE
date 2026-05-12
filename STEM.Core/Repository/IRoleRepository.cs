using STEM.Core.Entities.Users;

namespace STEM.Core.Repository;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}