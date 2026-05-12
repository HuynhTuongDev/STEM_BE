namespace STEM.Core.Entities.Projects;

public class Submission : BaseEntity
{
    public int AssignmentId { get; set; }
    public int FileId { get; set; }

    public Assignment? Assignment { get; set; }
    public FileEntity? File { get; set; }
}
