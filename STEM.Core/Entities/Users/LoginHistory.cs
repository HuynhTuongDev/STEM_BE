namespace STEM.Core.Entities.Users; public class LoginHistory : BaseEntity { public int UserId { get; set; } public DateTime LoginTime { get; set; } public User? User { get; set; } }
