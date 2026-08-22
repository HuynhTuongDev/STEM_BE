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

    // Alias để support cả 2 cách viết trong DB
    public const string SchoolAdmin = "School Admin";

    /// <summary>
    /// Roles that manage business operations (School Admin + Teacher)
    /// Used for accessing student data, classes, grades
    /// </summary>
    public static readonly HashSet<string> ManagementRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        SchoolAdministrator,
        SchoolAdmin,
        Teacher
    };

    /// <summary>
    /// Roles that can access school/business data (School Admin only, Master should not)
    /// </summary>
    public static readonly HashSet<string> SchoolDataAccessRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        SchoolAdministrator,
        SchoolAdmin
    };

    /// <summary>
    /// Check if role is Master Administrator
    /// </summary>
    public static bool IsMasterAdmin(string? roleName)
    {
        return string.Equals(roleName, MasterAdministrator, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if role is School Administrator (support both "School Administrator" and "School Admin")
    /// </summary>
    public static bool IsSchoolAdmin(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return false;
        return string.Equals(roleName, SchoolAdministrator, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleName, SchoolAdmin, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if role is a teacher
    /// </summary>
    public static bool IsTeacher(string? roleName)
    {
        return string.Equals(roleName, Teacher, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if role is a student
    /// </summary>
    public static bool IsStudent(string? roleName)
    {
        return string.Equals(roleName, Student, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if role name is a valid admin role (Master or School Admin)
    /// </summary>
    public static bool IsAdminRole(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return false;
        return string.Equals(roleName, MasterAdministrator, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleName, SchoolAdministrator, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleName, SchoolAdmin, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsManagementRole(string? roleName)
    {
        return !string.IsNullOrWhiteSpace(roleName) && ManagementRoles.Contains(roleName);
    }

    public static bool IsSchoolAdminOnly(string? roleName)
    {
        return !string.IsNullOrWhiteSpace(roleName) && SchoolDataAccessRoles.Contains(roleName);
    }
}
