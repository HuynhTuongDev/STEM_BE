namespace STEM.Application.Dtos.Users;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
}
