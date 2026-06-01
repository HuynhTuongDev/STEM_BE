namespace STEM.Application.Dtos.Schools;

public class SchoolRegistrationRequest
{
    // School info
    public string SchoolName { get; set; } = string.Empty;
    public string SchoolAddress { get; set; } = string.Empty;
    public string RepresentativeName { get; set; } = string.Empty;
    public string RepresentativeEmail { get; set; } = string.Empty;
    public string? ProofOfActivity { get; set; }

    // User (representative) info
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
