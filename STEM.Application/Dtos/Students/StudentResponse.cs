namespace STEM.Application.Dtos.Students;

public class StudentResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public int? SchoolId { get; set; }
    public string? SchoolName { get; set; }
    public int TotalEnrolledClasses { get; set; }
    public int CertificatesEarned { get; set; }
    public double? AverageScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
