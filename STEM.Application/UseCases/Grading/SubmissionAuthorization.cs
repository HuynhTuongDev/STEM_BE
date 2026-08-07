using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Users;

namespace STEM.Application.UseCases.Grading;

internal static class SubmissionAuthorization
{
    public static bool CanManageSubmission(User user, Submission submission)
    {
        var classEntity = submission.Assignment?.Class;
        if (classEntity == null)
        {
            return false;
        }

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

    public static bool CanViewSubmission(User user, Submission submission)
    {
        if (CanManageSubmission(user, submission))
        {
            return true;
        }

        return user.Role?.Name == RoleNames.Student && submission.StudentId == user.Id;
    }
}
