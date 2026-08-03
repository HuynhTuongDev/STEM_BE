namespace STEM.Application.Dtos.Schools;

public class UpdateSchoolRequest
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string RepresentativeName { get; set; } = string.Empty;
    public string RepresentativeEmail { get; set; } = string.Empty;
    public string? ProofOfActivity { get; set; }
    public string? StudentScale { get; set; }
    public string? RepresentativePosition { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }
    public string? Phone { get; set; }
}
