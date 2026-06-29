using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;

namespace STEM.Application.UseCases.Attendance;

internal static class AttendanceAuthorization
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
}
