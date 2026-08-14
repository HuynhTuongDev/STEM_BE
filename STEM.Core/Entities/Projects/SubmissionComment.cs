using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Projects;

public class SubmissionComment : BaseEntity
{
    public int SubmissionId { get; set; }
    public int AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;

    public Submission? Submission { get; set; }
    public User? Author { get; set; }
}
