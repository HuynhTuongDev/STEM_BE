namespace STEM.Core.Entities.Users; public class RefreshToken : BaseEntity { public int UserId { get; set; } public string Token { get; set; } = string.Empty; public User? User { get; set; } }
