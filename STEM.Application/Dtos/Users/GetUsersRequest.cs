namespace STEM.Application.Dtos.Users;

public class GetUsersRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public int? RoleId { get; set; }
    public bool? IsActive { get; set; }
    public int? SchoolId { get; set; }
}
