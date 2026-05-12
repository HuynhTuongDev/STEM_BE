using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Common;

public class Message : BaseEntity
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;

    public User? Sender { get; set; }
    public User? Receiver { get; set; }
}
