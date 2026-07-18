using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;
using STEM.Core.Entities.Courses;

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
    public string? ProofOfActivity { get; set; }
    public SchoolStatus Status { get; set; } = SchoolStatus.Pending;
    public string? StudentScale { get; set; }
    public string? RepresentativePosition { get; set; }
    public string? Website { get; set; }
    public string? Notes { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}