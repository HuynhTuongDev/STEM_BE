namespace STEM.Core.Entities.Projects;

public class AssignmentQuizDetail : BaseEntity
{
    public int AssignmentId { get; set; }
    public string QuestionsJson { get; set; } = "[]";
    public int? TimeLimitSeconds { get; set; }
    public bool ShuffleQuestions { get; set; }

    public Assignment? Assignment { get; set; }
}
