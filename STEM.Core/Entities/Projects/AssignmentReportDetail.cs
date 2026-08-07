namespace STEM.Core.Entities.Projects;

public class AssignmentReportDetail : BaseEntity
{
    public int AssignmentId { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public string AllowedSubmissionTypesJson { get; set; } = "[]";
    public string AllowedFileExtensionsJson { get; set; } = "[]";
    public int MaxFileSizeMb { get; set; } = 50;

    public Assignment? Assignment { get; set; }
}
