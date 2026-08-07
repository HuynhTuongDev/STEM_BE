namespace STEM.Application.Dtos.Schools;

public class SchoolRegistrationRequest
{
    // School info
    public string SchoolName { get; set; } = string.Empty;
    public string SchoolAddress { get; set; } = string.Empty;
    public string RepresentativeName { get; set; } = string.Empty;
    public string RepresentativeEmail { get; set; } = string.Empty;
    public string? ProofOfActivity { get; set; }
    public string? StudentScale { get; set; }
    public string? RepresentativePosition { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }

    // Document URL (uploaded certificate file)
    public string? DocumentUrl { get; set; }

    // User (representative) info
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
