using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Projects;

public class Submission : BaseEntity
{
    public int AssignmentId { get; set; }
    public int? StudentId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string Status { get; set; } = SubmissionStatuses.Submitted;
    public string ContentJson { get; set; } = "{}";
    public string? AutoGradeResultJson { get; set; }
    public decimal? AutoScore { get; set; }
    public decimal? FinalScore { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public int? FileId { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
    public int? GradedById { get; set; }
    public DateTime? GradedAt { get; set; }

    public Assignment? Assignment { get; set; }
    public User? Student { get; set; }
    public FileEntity? File { get; set; }
    public User? GradedBy { get; set; }
}
