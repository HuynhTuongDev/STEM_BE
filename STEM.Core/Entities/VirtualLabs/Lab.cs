namespace STEM.Core.Entities.VirtualLabs;

using STEM.Core.Entities.Common;
using STEM.Core.Entities.Users;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Projects;

public class Lab : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string SimulationMode { get; set; } = string.Empty;
    public string BoardType { get; set; } = string.Empty;
    public string? CircuitConfigJson { get; set; }
    public string? AllowedComponentTypesJson { get; set; }
    public string? WokwiProjectId { get; set; }
    public string? WokwiProjectUrl { get; set; }
    public string Status { get; set; } = "Draft";
    public int CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public int? LinkedAssignmentId { get; set; }
    public Assignment? LinkedAssignment { get; set; }
    public ICollection<LabClassAssignment> ClassAssignments { get; set; } = new List<LabClassAssignment>();
    public ICollection<LabProgress> Progresses { get; set; } = new List<LabProgress>();
    public ICollection<VirtualLabProject> Projects { get; set; } = new List<VirtualLabProject>();
}

public class VirtualLabProject : BaseEntity
{
    public int LabId { get; set; }
    public Lab? Lab { get; set; }
    public int StudentId { get; set; }
    public User? Student { get; set; }
    public string? CircuitJson { get; set; }
    public string? PythonCode { get; set; }
    public string? SimulationEventsJson { get; set; }
    public string Status { get; set; } = "InProgress";
    public int? Score { get; set; }
    public string? Feedback { get; set; }
}

public class LabClassAssignment : BaseEntity
{
    public int LabId { get; set; }
    public Lab? Lab { get; set; }
    public int ClassId { get; set; }
    public Class? Class { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public int? MaxAttempts { get; set; }
}

public class LabProgress : BaseEntity
{
    public int LabId { get; set; }
    public Lab? Lab { get; set; }
    public int StudentId { get; set; }
    public User? Student { get; set; }
    public int Attempts { get; set; } = 0;
    public int? BestScore { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public string? LastCode { get; set; }
    public string? LastCircuitJson { get; set; }
}

public class ComponentGlueRegistry : BaseEntity
{
    public string ComponentType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Supported { get; set; } = true;
    public string? PinRequirementsJson { get; set; }
}
