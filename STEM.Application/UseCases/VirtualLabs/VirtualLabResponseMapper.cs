using STEM.Application.Dtos.VirtualLabs;
using STEM.Core.Entities.Simulations;

namespace STEM.Application.UseCases.VirtualLabs;

internal static class VirtualLabResponseMapper
{
    public static VirtualLabResponse Map(SimulationTemplate template)
    {
        var simulation = template.Simulation;
        var classEntity = simulation?.Class;
        var course = classEntity?.Course;

        return new VirtualLabResponse
        {
            Id = template.Id,
            SimulationId = template.SimulationId,
            ClassId = classEntity?.Id ?? 0,
            ClassCode = classEntity?.ClassCode ?? string.Empty,
            CourseId = course?.Id ?? 0,
            CourseTitle = course?.Title ?? string.Empty,
            TeacherId = classEntity?.TeacherId ?? 0,
            TeacherName = classEntity?.Teacher?.FullName ?? string.Empty,
            SchoolId = classEntity?.SchoolId ?? 0,
            SchoolName = classEntity?.School?.Name ?? string.Empty,
            SimulationName = template.SimulationName,
            Description = template.Description,
            DiagramJson = template.Config,
            SessionsCount = template.SimulationSessions.Count,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}
