using System.Text.Json;
using STEM.Application.Dtos.Assignments;
using STEM.Core.Entities.Projects;

namespace STEM.Application.UseCases.Assignments;

internal static class AssignmentRequestMapper
{
    public static void ValidateBase(int classId, string title, string assignmentType, string status, decimal maxScore)
    {
        if (classId <= 0)
            throw new ArgumentException("ClassId is required.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        if (!AssignmentTypes.All.Contains(assignmentType))
            throw new ArgumentException("AssignmentType must be quiz, text_report, or practical_simulation.");

        if (!AssignmentStatuses.All.Contains(status))
            throw new ArgumentException("Status must be draft, published, or closed.");

        if (maxScore <= 0)
            throw new ArgumentException("MaxScore must be greater than 0.");
    }

    public static void ApplyBase(Assignment assignment, CreateAssignmentRequest request, int currentUserId, DateTime now)
    {
        assignment.ClassId = request.ClassId;
        assignment.Title = request.Title.Trim();

        // For text_report, use default template if description is empty
        if (request.AssignmentType.Equals(AssignmentTypes.TextReport, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.Description))
        {
            assignment.Description = DefaultAssignmentInstructions.IoTReport;
        }
        else
        {
            assignment.Description = request.Description.Trim();
        }

        assignment.AssignmentType = request.AssignmentType.Trim();
        assignment.DueDate = request.DueDate;
        assignment.MaxScore = request.MaxScore;
        assignment.RubricId = request.RubricId;
        assignment.AllowResubmit = request.AllowResubmit;
        assignment.ResubmitLimit = request.ResubmitLimit;
        assignment.Status = request.Status.Trim();
        assignment.CreatedById = currentUserId;
        assignment.CreatedAt = now;
        assignment.UpdatedAt = now;
    }

    public static void ApplyBase(Assignment assignment, UpdateAssignmentRequest request, DateTime now)
    {
        assignment.ClassId = request.ClassId;
        assignment.Title = request.Title.Trim();

        // For text_report, use default template if description is empty
        if (request.AssignmentType.Equals(AssignmentTypes.TextReport, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.Description))
        {
            assignment.Description = DefaultAssignmentInstructions.IoTReport;
        }
        else
        {
            assignment.Description = request.Description.Trim();
        }

        assignment.AssignmentType = request.AssignmentType.Trim();
        assignment.DueDate = request.DueDate;
        assignment.MaxScore = request.MaxScore;
        assignment.RubricId = request.RubricId;
        assignment.AllowResubmit = request.AllowResubmit;
        assignment.ResubmitLimit = request.ResubmitLimit;
        assignment.Status = request.Status.Trim();
        assignment.UpdatedAt = now;
    }

    public static void ApplyDetails(Assignment assignment, CreateAssignmentRequest request, DateTime now)
    {
        ClearDetails(assignment);

        if (request.AssignmentType.Equals(AssignmentTypes.Quiz, StringComparison.OrdinalIgnoreCase))
        {
            var detail = request.QuizDetail ?? throw new ArgumentException("QuizDetail is required for quiz assignments.");
            assignment.QuizDetail = new AssignmentQuizDetail
            {
                QuestionsJson = ToJson(detail.Questions, "[]"),
                TimeLimitSeconds = detail.TimeLimitSeconds,
                ShuffleQuestions = detail.ShuffleQuestions,
                CreatedAt = now,
                UpdatedAt = now
            };
            return;
        }

        if (request.AssignmentType.Equals(AssignmentTypes.TextReport, StringComparison.OrdinalIgnoreCase))
        {
            var detail = request.ReportDetail ?? throw new ArgumentException("ReportDetail is required for text_report assignments.");
            var instructions = string.IsNullOrWhiteSpace(detail.Instructions)
                ? DefaultAssignmentInstructions.DefaultReport
                : detail.Instructions;
            assignment.ReportDetail = new AssignmentReportDetail
            {
                Instructions = instructions.Trim(),
                AllowedSubmissionTypesJson = JsonSerializer.Serialize(detail.AllowedSubmissionTypes),
                AllowedFileExtensionsJson = JsonSerializer.Serialize(detail.AllowedFileExtensions),
                MaxFileSizeMb = detail.MaxFileSizeMb,
                CreatedAt = now,
                UpdatedAt = now
            };
            return;
        }

        var simulationDetail = request.SimulationDetail ?? throw new ArgumentException("SimulationDetail is required for practical_simulation assignments.");
        assignment.SimulationDetail = new AssignmentSimulationDetail
        {
            EnvironmentSource = simulationDetail.EnvironmentSource.Trim(),
            BaseDiagramJson = ToJson(simulationDetail.BaseDiagram, "{}"),
            AllowedComponentTypesJson = JsonSerializer.Serialize(simulationDetail.AllowedComponentTypes),
            StudentInputMode = simulationDetail.StudentInputMode.Trim(),
            StarterCode = string.IsNullOrWhiteSpace(simulationDetail.StarterCode) ? null : simulationDetail.StarterCode,
            AnswerKeyJson = ToJson(simulationDetail.AnswerKey, "{}"),
            AutoGradingEnabled = simulationDetail.AutoGradingEnabled,
            AutoGradingWeight = simulationDetail.AutoGradingWeight,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static void ApplyDetails(Assignment assignment, UpdateAssignmentRequest request, DateTime now)
    {
        var createLikeRequest = new CreateAssignmentRequest
        {
            ClassId = request.ClassId,
            Title = request.Title,
            Description = request.Description,
            AssignmentType = request.AssignmentType,
            DueDate = request.DueDate,
            MaxScore = request.MaxScore,
            RubricId = request.RubricId,
            AllowResubmit = request.AllowResubmit,
            ResubmitLimit = request.ResubmitLimit,
            Status = request.Status,
            QuizDetail = request.QuizDetail,
            ReportDetail = request.ReportDetail,
            SimulationDetail = request.SimulationDetail
        };

        ApplyDetails(assignment, createLikeRequest, now);
    }

    private static void ClearDetails(Assignment assignment)
    {
        assignment.QuizDetail = null;
        assignment.ReportDetail = null;
        assignment.SimulationDetail = null;
    }

    private static string ToJson(JsonElement value, string fallback)
    {
        return value.ValueKind == JsonValueKind.Undefined ? fallback : value.GetRawText();
    }
}
