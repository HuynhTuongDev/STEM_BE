using STEM.Core.Entities.Users;

namespace STEM.Core.Repository;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? roleId,
        bool? isActive,
        int? schoolId,
        CancellationToken cancellationToken = default);
}
