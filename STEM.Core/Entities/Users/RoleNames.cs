namespace STEM.Core.Entities.Users;

/// <summary>
/// Centralized role definitions for STEM platform
/// 
/// Role Hierarchy:
/// - Master Administrator: System developer/operator - can only access system admin tasks, NOT student/school data
/// - School Administrator: School management - can access all school data (students, classes, grades, etc.)
/// - Teacher: Teaching role - can manage classes and grade students
/// - Student: Learning role - can access courses and assignments
/// </summary>
public static class RoleNames
{
    public const string MasterAdministrator = "Master Administrator";
    public const string SchoolAdministrator = "School Administrator";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    /// <summary>
    /// Roles that manage business operations (School Admin + Teacher)
    /// Used for accessing student data, classes, grades
    /// </summary>
    public static readonly HashSet<string> ManagementRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        SchoolAdministrator,
        Teacher
    };

    /// <summary>
    /// Roles that can access school/business data (School Admin only, Master should not)
    /// </summary>
    public static readonly HashSet<string> SchoolDataAccessRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        SchoolAdministrator
    };

    public static bool IsManagementRole(string? roleName)
    {
        return !string.IsNullOrWhiteSpace(roleName) && ManagementRoles.Contains(roleName);
    }

    public static bool IsSchoolAdminOnly(string? roleName)
    {
        return !string.IsNullOrWhiteSpace(roleName) && SchoolDataAccessRoles.Contains(roleName);
    }
}
