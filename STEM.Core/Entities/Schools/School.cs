using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Schools;

public enum SchoolStatus
{
    Pending,
    Approved,
    Rejected
}

public class School : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string RepresentativeEmail { get; set; } = string.Empty;
    public string RepresentativeName { get; set; } = string.Empty;
    public string? ProofOfActivity { get; set; } // URL to document
    public SchoolStatus Status { get; set; } = SchoolStatus.Pending;

    // Navigation property for users belonging to this school
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Classes.Class> Classes { get; set; } = new List<Classes.Class>();
}