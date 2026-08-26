using STEM.Core.Entities.Payments;
using STEM.Core.Repository;

namespace STEM.Core.Interfaces;

public interface ITokenAllocationRepository : IRepository<TokenAllocation>
{
    Task<IEnumerable<TokenAllocation>> GetByAccountIdAsync(int accountId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<TokenAllocation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<TokenAllocation?> GetActiveAllocationAsync(int accountId, int userId, CancellationToken cancellationToken = default);
    Task<int> GetCountByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<int> GetTotalAllocatedByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<TokenAllocation?> GetByIdWithUserAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TokenAllocation>> GetExpiredAllocationsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TokenAllocation>> GetExpiredByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<(int TeacherCount, int StudentCount, int TeacherTokens, int StudentTokens)> GetAllocationStatsByRoleAsync(int accountId, CancellationToken cancellationToken = default);
}
