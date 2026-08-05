namespace STEM.Core.Entities.Projects;

public static class AssignmentTypes
{
    public const string Quiz = "quiz";
    public const string TextReport = "text_report";
    public const string PracticalSimulation = "practical_simulation";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Quiz,
        TextReport,
        PracticalSimulation
    };
}

public static class AssignmentStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Closed = "closed";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Draft,
        Published,
        Closed
    };
}

public static class SubmissionStatuses
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
    public const string Graded = "graded";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Draft,
        Submitted,
        Graded
    };
}
