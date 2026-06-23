using System.Collections.Generic;

namespace STEM.Application.Dtos.Users;

public class PagedUserListResponse
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<UserListItemResponse> Items { get; set; } = new();
}
