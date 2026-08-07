using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Classes;

public class Enrollment : BaseEntity
{
    public int ClassId { get; set; }
    public int StudentId { get; set; }
    public DateTime EnrolledAt { get; set; }
    
    public Class? Class { get; set; }
    public User? Student { get; set; }
}
