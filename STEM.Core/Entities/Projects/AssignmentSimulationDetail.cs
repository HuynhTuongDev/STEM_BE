namespace STEM.Core.Entities.Projects;

public class AssignmentSimulationDetail : BaseEntity
{
    public int AssignmentId { get; set; }
    public string EnvironmentSource { get; set; } = "internal_sandbox";
    public string BaseDiagramJson { get; set; } = "{}";
    public string AllowedComponentTypesJson { get; set; } = "[]";
    public string StudentInputMode { get; set; } = "circuit_build";
    public string? StarterCode { get; set; }
    public string AnswerKeyJson { get; set; } = "{}";
    public bool AutoGradingEnabled { get; set; }
    public double AutoGradingWeight { get; set; } = 1;

    public Assignment? Assignment { get; set; }
}
