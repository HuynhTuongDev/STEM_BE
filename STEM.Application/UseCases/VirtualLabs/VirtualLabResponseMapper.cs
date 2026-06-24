using STEM.Application.Dtos.VirtualLabs;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.UseCases.VirtualLabs;

internal static class VirtualLabResponseMapper
{
    public static VirtualLabResponse Map(SimulationTemplate template)
    {
        var simulation = template.Simulation;
        var lesson = simulation?.Lesson;
        var course = lesson?.Module?.Course;

        return new VirtualLabResponse
        {
            Id = template.Id,
            SimulationId = template.SimulationId,
            LessonId = simulation?.LessonId ?? 0,
            LessonTitle = lesson?.Title ?? string.Empty,
            CourseId = course?.Id ?? 0,
            CourseTitle = course?.Title ?? string.Empty,
            TeacherId = course?.TeacherId ?? 0,
            TeacherName = course?.Teacher?.FullName ?? string.Empty,
            SchoolId = course?.SchoolId,
            SchoolName = course?.School?.Name,
            SimulationName = template.SimulationName,
            Description = template.Description,
            DiagramJson = template.Config,
            SessionsCount = template.SimulationSessions.Count,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}
