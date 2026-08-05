using STEM.Core.Entities.Classes;

namespace STEM.Core.Entities.Projects;

public class Assignment : BaseEntity
{
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AssignmentType { get; set; } = AssignmentTypes.TextReport;
    public DateTime? DueDate { get; set; }
    public decimal MaxScore { get; set; } = 100;
    public int? RubricId { get; set; }
    public bool AllowResubmit { get; set; }
    public int? ResubmitLimit { get; set; }
    public string Status { get; set; } = AssignmentStatuses.Draft;
    public int? CreatedById { get; set; }

    public Class? Class { get; set; }
    public AssignmentQuizDetail? QuizDetail { get; set; }
    public AssignmentReportDetail? ReportDetail { get; set; }
    public AssignmentSimulationDetail? SimulationDetail { get; set; }
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<Metric> Metrics { get; set; } = new List<Metric>();
}
