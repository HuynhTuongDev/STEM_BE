using STEM.Core.Entities.Common;

namespace STEM.Core.Entities.Users;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int RoleId { get; set; }

    public Role? Role { get; set; }

    // Navigation properties cho Messages
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
}
