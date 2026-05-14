namespace STEM.Application.Dtos.LoginHistory;

public class LoginHistoryResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime LoginTime { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
