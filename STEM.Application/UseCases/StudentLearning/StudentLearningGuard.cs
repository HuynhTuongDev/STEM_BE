using STEM.Core.Entities.Projects;
using STEM.Core.Entities.Quizzes;
using STEM.Core.Entities.Users;

namespace STEM.Application.UseCases.StudentLearning;

internal static class StudentLearningGuard
{
    public static void EnsureStudent(User? user)
    {
        if (user == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        if (user.Role?.Name != RoleNames.Student)
        {
            throw new UnauthorizedAccessException("Only students can use this endpoint.");
        }
    }

    public static bool CanAccessAssignment(Assignment assignment, int studentId)
    {
        return assignment.Class?.Enrollments.Any(e => e.StudentId == studentId) == true;
    }

    public static bool CanAccessQuiz(Quiz quiz, int studentId)
    {
        return quiz.Course?.Classes.Any(c => c.Enrollments.Any(e => e.StudentId == studentId)) == true;
    }
}
