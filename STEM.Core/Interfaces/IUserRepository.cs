using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;

namespace STEM.Core.Repository;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        int? roleId,
        bool? isActive,
        int? schoolId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<User>> GetStudentsNotInClassAsync(int classId, int schoolId, string? searchTerm, CancellationToken cancellationToken = default);
    Task<IEnumerable<Schedule>> GetStudentSchedulesAsync(int studentId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Users, int TotalCount)> GetTeachersWithClassCountAsync(int schoolId, int page, int pageSize, string? searchTerm, CancellationToken cancellationToken = default);
}
