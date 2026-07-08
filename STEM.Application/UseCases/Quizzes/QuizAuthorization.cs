using STEM.Application.Dtos.Quizzes;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Users;

namespace STEM.Application.UseCases.Quizzes;

internal static class QuizAuthorization
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

    public static bool CanViewQuiz(User user, Quiz quiz)
    {
        var roleName = user.Role?.Name;
        var classEntity = quiz.Class;
        if (classEntity == null)
        {
            return false;
        }

        if (roleName == RoleNames.SchoolAdministrator)
        {
            return user.SchoolId.HasValue && classEntity.SchoolId == user.SchoolId.Value;
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
