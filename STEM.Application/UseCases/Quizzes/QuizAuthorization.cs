using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Entities.Users;

namespace STEM.Application.UseCases.Quizzes;

internal static class QuizAuthorization
{
    public static bool CanManageCourse(User user, Course course)
    {
        var roleName = user.Role?.Name;

        if (roleName == RoleNames.SchoolAdministrator)
        {
            return user.SchoolId.HasValue && course.SchoolId == user.SchoolId.Value;
        }

        if (roleName == RoleNames.Teacher)
        {
            return course.TeacherId == user.Id;
        }

        return false;
    }

    public static bool CanViewQuiz(User user, Quiz quiz)
    {
        var roleName = user.Role?.Name;
        var course = quiz.Course;
        if (course == null)
        {
            return false;
        }

        if (roleName == RoleNames.SchoolAdministrator)
        {
            return user.SchoolId.HasValue && course.SchoolId == user.SchoolId.Value;
        }

        if (roleName == RoleNames.Teacher)
        {
            return course.TeacherId == user.Id;
        }

        if (roleName == RoleNames.Student)
        {
            return course.Classes.Any(classEntity =>
                classEntity.Enrollments.Any(enrollment => enrollment.StudentId == user.Id));
        }

        return false;
    }
}
