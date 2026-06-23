namespace STEM.Core.Entities.Users;

public class LoginHistory : BaseEntity
{
    public int UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;

    public User? User { get; set; }
}
