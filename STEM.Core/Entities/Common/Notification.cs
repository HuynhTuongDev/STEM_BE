using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Common;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;

    public User? User { get; set; }
}
