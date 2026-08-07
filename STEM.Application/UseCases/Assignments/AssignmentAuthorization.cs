using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Users;

namespace STEM.Application.UseCases.Assignments;

internal static class AssignmentAuthorization
{
    public static bool CanManageClass(User user, Class classEntity)
    {
        var roleName = user.Role?.Name;

        if (roleName == RoleNames.SchoolAdministrator)
        {
            return user.SchoolId.HasValue && user.SchoolId.Value == classEntity.SchoolId;
        }

        if (roleName == RoleNames.Teacher)
        {
            return classEntity.TeacherId == user.Id;
        }

        return false;
    }

    public static bool CanViewAssignment(User user, Assignment assignment)
    {
        var roleName = user.Role?.Name;
        var classEntity = assignment.Class;
        if (classEntity == null)
        {
            return false;
        }

        if (roleName == RoleNames.SchoolAdministrator)
        {
            return user.SchoolId.HasValue && user.SchoolId.Value == classEntity.SchoolId;
        }

        if (roleName == RoleNames.Teacher)
        {
            return classEntity.TeacherId == user.Id;
        }

        if (roleName == RoleNames.Student)
        {
            return classEntity.Enrollments.Any(enrollment => enrollment.StudentId == user.Id);
        }

        return false;
    }
}
