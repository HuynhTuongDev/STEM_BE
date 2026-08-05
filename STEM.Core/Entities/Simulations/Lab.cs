using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Simulations;

public class Lab
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = LabCategories.Robotics;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string SimulationMode { get; set; } = LabSimulationModes.WokwiIframe;
    public string BoardType { get; set; } = LabBoardTypes.ArduinoUno;
    public string? StarterCode { get; set; }
    public string CircuitConfigJson { get; set; } = "{}";
    public string AllowedComponentTypesJson { get; set; } = "[]";
    public string? WokwiProjectId { get; set; }
    public string? WokwiProjectUrl { get; set; }
    public int CreatedById { get; set; }
    public int? LinkedAssignmentId { get; set; }
    public string Status { get; set; } = LabStatuses.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? CreatedBy { get; set; }
    public Assignment? LinkedAssignment { get; set; }
    public ICollection<LabClassAssignment> ClassAssignments { get; set; } = new List<LabClassAssignment>();
    public ICollection<LabProgress> Progresses { get; set; } = new List<LabProgress>();
}

public static class LabCategories
{
    public const string Physics = "physics";
    public const string Chemistry = "chemistry";
    public const string Biology = "biology";
    public const string Robotics = "robotics";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Physics,
        Chemistry,
        Biology,
        Robotics
    };
}

public static class LabStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Draft,
        Published
    };
}

public static class LabSimulationModes
{
    public const string WokwiIframe = "wokwi_iframe";
    public const string CustomSandbox = "custom_sandbox";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        WokwiIframe,
        CustomSandbox
    };
}

public static class LabBoardTypes
{
    public const string ArduinoUno = "arduino_uno";
    public const string Esp32DevkitV1 = "esp32_devkit_v1";

    // Must stay in sync with the frontend LabBoardType union.
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        ArduinoUno,
        Esp32DevkitV1
    };
}
