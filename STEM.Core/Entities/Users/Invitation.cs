using STEM.Core.Entities.Schools;

namespace STEM.Core.Entities.Users;

public class Invitation : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int SchoolId { get; set; }
    public string RoleName { get; set; } = RoleNames.Teacher;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }

    public School? School { get; set; }
}