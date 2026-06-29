using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Projects;

namespace STEM.Application.UseCases.Assignments;

internal static class AssignmentResponseMapper
{
    public static AssignmentResponse Map(Assignment assignment)
    {
        var classEntity = assignment.Class;
        var course = classEntity?.Course;
        var teacher = classEntity?.Teacher;
        var school = classEntity?.School;

        return new AssignmentResponse
        {
            Id = assignment.Id,
            ClassId = assignment.ClassId,
            ClassCode = classEntity?.ClassCode ?? string.Empty,
            CourseId = classEntity?.CourseId ?? 0,
            CourseTitle = course?.Title ?? string.Empty,
            TeacherId = classEntity?.TeacherId ?? 0,
            TeacherName = teacher?.FullName ?? string.Empty,
            SchoolId = classEntity?.SchoolId ?? 0,
            SchoolName = school?.Name ?? string.Empty,
            Title = assignment.Title,
            SubmissionCount = assignment.Submissions.Count,
            MetricCount = assignment.Metrics.Count,
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt
        };
    }
}
