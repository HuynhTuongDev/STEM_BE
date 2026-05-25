using STEM.Core.Entities.Common;
using STEM.Core.Entities.Schools;

namespace STEM.Core.Entities.Users;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }

    public bool IsActive { get; set; }
    public int RoleId { get; set; }
    public int? SchoolId { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpires { get; set; }

    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }

    public Role? Role { get; set; }
    public School? School { get; set; }

    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

}
