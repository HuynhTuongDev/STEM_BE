using STEM.Core.Entities.Courses;
using STEM.Core.Entities.Simulations;
using STEM.Core.Entities.Users;

namespace STEM.Application.UseCases.VirtualLabs;

internal static class VirtualLabAuthorization
{
    public static bool CanManageLesson(User user, Lesson lesson)
    {
        var course = lesson.Module?.Course;
        return course != null && CanManageCourse(user, course);
    }

    public static bool CanManageLab(User user, SimulationTemplate template)
    {
        var course = template.Simulation?.Lesson?.Module?.Course;
        return course != null && CanManageCourse(user, course);
    }

    public static bool CanViewLab(User user, SimulationTemplate template)
    {
        var course = template.Simulation?.Lesson?.Module?.Course;
        if (course == null)
        {
            return false;
        }

        if (CanManageCourse(user, course))
        {
            return true;
        }

        return user.Role?.Name == RoleNames.Student &&
            course.Classes.Any(classEntity =>
                classEntity.Enrollments.Any(enrollment => enrollment.StudentId == user.Id));
    }

    private static bool CanManageCourse(User user, Course course)
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
}
