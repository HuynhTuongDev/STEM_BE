using System.Text.Json;
using System.Text.Json.Nodes;
using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Classes;
using STEM.Core.Entities.Projects;

namespace STEM.Application.UseCases.Assignments;

internal static class AssignmentResponseMapper
{
    /// <summary>
    /// Maps an Assignment to its response DTO. <paramref name="revealAnswers"/> must be
    /// false for Student callers — GET /Assignments/{id} is shared by Teacher and Student,
    /// and Quiz options[].isCorrect / fill_blank correctAnswer / Simulation AnswerKeyJson
    /// are graded-answer data that must not reach a Student before/while they can still submit.
    /// </summary>
    public static AssignmentResponse Map(Assignment assignment, Class? classEntityFromParam = null, bool revealAnswers = true, int? currentStudentId = null)
    {
        var classEntity = classEntityFromParam ?? assignment.Class;
        var course = classEntity?.Course;
        var teacher = classEntity?.Teacher;
        var school = classEntity?.School;

        // Calculate submission info for students
        bool hasSubmitted = false;
        decimal? highestScore = null;
        int? lastAttemptNumber = null;
        bool canResubmit = true;

        if (currentStudentId.HasValue && assignment.Submissions != null)
        {
            var studentSubmissions = assignment.Submissions
                .Where(s => s.StudentId == currentStudentId.Value)
                .ToList();

            if (studentSubmissions.Any())
            {
                hasSubmitted = true;
                lastAttemptNumber = studentSubmissions.Max(s => s.AttemptNumber);
                highestScore = studentSubmissions
                    .Where(s => s.FinalScore.HasValue || s.Score.HasValue || s.AutoScore.HasValue)
                    .Select(s => s.FinalScore ?? s.Score ?? s.AutoScore)
                    .Max();

                if (!assignment.AllowResubmit)
                {
                    canResubmit = false;
                }
                else if (assignment.ResubmitLimit.HasValue)
                {
                    canResubmit = studentSubmissions.Count < assignment.ResubmitLimit.Value;
                }
            }
        }

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
            RubricCriteria = assignment.Rubric == null ? null : ParseRubricCriteria(assignment.Rubric.Criteria),
            AllowResubmit = assignment.AllowResubmit,
            ResubmitLimit = assignment.ResubmitLimit,
            Status = assignment.Status,
            CreatedById = assignment.CreatedById,
            QuizDetail = assignment.QuizDetail == null ? null : new AssignmentQuizDetailResponse
            {
                Questions = revealAnswers
                    ? ParseJson(assignment.QuizDetail.QuestionsJson, "[]")
                    : StripQuizAnswers(ParseJson(assignment.QuizDetail.QuestionsJson, "[]")),
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
                AnswerKey = revealAnswers
                    ? ParseJson(assignment.SimulationDetail.AnswerKeyJson, "{}")
                    : ParseJson(null, "{}"),
                AutoGradingEnabled = assignment.SimulationDetail.AutoGradingEnabled,
                AutoGradingWeight = assignment.SimulationDetail.AutoGradingWeight
            },
            SubmissionCount = assignment.Submissions.Count,
            MetricCount = assignment.Metrics.Count,
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt,
            HasSubmitted = hasSubmitted,
            HighestScore = highestScore,
            LastAttemptNumber = lastAttemptNumber,
            CanResubmit = canResubmit
        };
    }

    private static JsonElement ParseJson(string? json, string fallback)
    {
        return JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(json) ? fallback : json);
    }

    // Removes graded-answer fields (options[].isCorrect, correctAnswer) from the raw
    // Questions JSON before it reaches a Student caller, leaving question text/type/options
    // text intact so the quiz is still fully viewable/answerable.
    private static JsonElement StripQuizAnswers(JsonElement questions)
    {
        var node = JsonNode.Parse(questions.GetRawText());
        if (node is JsonArray questionArray)
        {
            foreach (var questionNode in questionArray)
            {
                if (questionNode is not JsonObject question)
                {
                    continue;
                }

                question.Remove("correctAnswer");

                if (question["options"] is JsonArray options)
                {
                    foreach (var optionNode in options)
                    {
                        if (optionNode is JsonObject option)
                        {
                            option.Remove("isCorrect");
                        }
                    }
                }
            }
        }

        return JsonSerializer.Deserialize<JsonElement>(node?.ToJsonString() ?? "[]");
    }

    private static List<RubricCriterionResponse>? ParseRubricCriteria(string? criteriaJson)
    {
        if (string.IsNullOrWhiteSpace(criteriaJson))
            return null;

        try
        {
            var criteria = JsonSerializer.Deserialize<List<RubricCriterionResponse>>(criteriaJson);
            return criteria;
        }
        catch
        {
            return null;
        }
    }
}
