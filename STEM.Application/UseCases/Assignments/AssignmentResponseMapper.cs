using System.Text.Json;
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
            Description = assignment.Description,
            AssignmentType = assignment.AssignmentType,
            DueDate = assignment.DueDate,
            MaxScore = assignment.MaxScore,
            RubricId = assignment.RubricId,
            AllowResubmit = assignment.AllowResubmit,
            ResubmitLimit = assignment.ResubmitLimit,
            Status = assignment.Status,
            CreatedById = assignment.CreatedById,
            QuizDetail = assignment.QuizDetail == null ? null : new AssignmentQuizDetailResponse
            {
                Questions = ParseJson(assignment.QuizDetail.QuestionsJson, "[]"),
                TimeLimitSeconds = assignment.QuizDetail.TimeLimitSeconds,
                ShuffleQuestions = assignment.QuizDetail.ShuffleQuestions
            },
            ReportDetail = assignment.ReportDetail == null ? null : new AssignmentReportDetailResponse
            {
                Instructions = assignment.ReportDetail.Instructions,
                AllowedSubmissionTypes = ParseJson(assignment.ReportDetail.AllowedSubmissionTypesJson, "[]"),
                AllowedFileExtensions = ParseJson(assignment.ReportDetail.AllowedFileExtensionsJson, "[]"),
                MaxFileSizeMb = assignment.ReportDetail.MaxFileSizeMb
            },
            SimulationDetail = assignment.SimulationDetail == null ? null : new AssignmentSimulationDetailResponse
            {
                EnvironmentSource = assignment.SimulationDetail.EnvironmentSource,
                BaseDiagram = ParseJson(assignment.SimulationDetail.BaseDiagramJson, "{}"),
                AllowedComponentTypes = ParseJson(assignment.SimulationDetail.AllowedComponentTypesJson, "[]"),
                StudentInputMode = assignment.SimulationDetail.StudentInputMode,
                StarterCode = assignment.SimulationDetail.StarterCode,
                AnswerKey = ParseJson(assignment.SimulationDetail.AnswerKeyJson, "{}"),
                AutoGradingEnabled = assignment.SimulationDetail.AutoGradingEnabled,
                AutoGradingWeight = assignment.SimulationDetail.AutoGradingWeight
            },
            SubmissionCount = assignment.Submissions.Count,
            MetricCount = assignment.Metrics.Count,
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt
        };
    }

    private static JsonElement ParseJson(string? json, string fallback)
    {
        return JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(json) ? fallback : json);
    }
}
