namespace STEM.Application.Dtos.Students;

public class StudentGradeResponse
{
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateTime GradedAt { get; set; }
}
