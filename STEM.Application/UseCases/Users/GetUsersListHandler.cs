using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using STEM.Application.Dtos.Users;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Users;

public class GetUsersListHandler
{
    private readonly IUserRepository _userRepository;

    public GetUsersListHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedUserListResponse> Handle(
        GetUsersRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Retrieve the current user from database to ensure up-to-date role and school information
        var adminUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (adminUser == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        var roleName = adminUser.Role?.Name;

        // Enforce basic role check
        if (roleName != RoleNames.MasterAdministrator && roleName != RoleNames.SchoolAdministrator)
        {
            throw new UnauthorizedAccessException("Only administrators can view the users list.");
        }

        // Data Isolation: If School Administrator, restrict query to their own school only.
        int? filterSchoolId = request.SchoolId;
        if (roleName == RoleNames.SchoolAdministrator)
        {
            filterSchoolId = adminUser.SchoolId;
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var (users, totalCount) = await _userRepository.GetUsersPagedAsync(
            pageNumber,
            pageSize,
            request.SearchTerm,
            request.RoleId,
            request.IsActive,
            filterSchoolId,
            cancellationToken
        );

        var items = users.Select(u => new UserListItemResponse
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            Phone = u.Phone,
            Avatar = u.Avatar,
            IsActive = u.IsActive,
            RoleId = u.RoleId,
            RoleName = u.Role?.Name ?? string.Empty,
            SchoolId = u.SchoolId,
            SchoolName = u.School?.Name,
            IsEmailVerified = u.IsEmailVerified,
            CreatedAt = u.CreatedAt
        }).ToList();

        return new PagedUserListResponse
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }
}
