using STEM.Core.Entities.Users;

namespace STEM.Core.Repository;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<IEnumerable<Permission>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
}