using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Users;

namespace STEM.Application.UseCases.Grading;

internal static class ResubmitRequestAuthorization
{
    public static bool CanReview(User user, ResubmitRequest request)
    {
        var classEntity = request.Assignment?.Class;
        if (classEntity == null)
        {
            return false;
        }

        var roleName = user.Role?.Name;
        if (roleName == RoleNames.Teacher)
        {
            return classEntity.TeacherId == user.Id;
        }

        if (roleName == RoleNames.SchoolAdministrator)
        {
            return user.SchoolId.HasValue && user.SchoolId.Value == classEntity.SchoolId;
        }

        return false;
    }
}
