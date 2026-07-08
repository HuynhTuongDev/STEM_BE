using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;

namespace STEM.Application.UseCases.VirtualLabs;

internal static class VirtualLabAuthorization
{
    public static bool CanManageClass(User user, Class classEntity)
    {
        var roleName = user.Role?.Name;

        if (roleName == RoleNames.SchoolAdministrator)
        {
            return user.SchoolId.HasValue && classEntity.SchoolId == user.SchoolId.Value;
        }

        if (roleName == RoleNames.Teacher)
        {
            return classEntity.TeacherId == user.Id;
        }

        return false;
    }

    public static bool CanManageLab(User user, SimulationTemplate template)
    {
        var classEntity = template.Simulation?.Class;
        return classEntity != null && CanManageClass(user, classEntity);
    }

    public static bool CanViewLab(User user, SimulationTemplate template)
    {
        var classEntity = template.Simulation?.Class;
        if (classEntity == null)
        {
            return false;
        }

        if (CanManageClass(user, classEntity))
        {
            return true;
        }

        return user.Role?.Name == RoleNames.Student &&
            classEntity.Enrollments.Any(enrollment => enrollment.StudentId == user.Id);
    }
}
