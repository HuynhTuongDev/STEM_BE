using System.Text.Json;
using STEM.Application.Dtos.Grading;
using STEM.Core.Entities.Projects;

namespace STEM.Application.UseCases.Grading;

internal static class SubmissionResponseMapper
{
    public static SubmissionResponse Map(Submission submission)
    {
        var assignment = submission.Assignment;
        var classEntity = assignment?.Class;
        var student = submission.Student;
        var gradedBy = submission.GradedBy;

        return new SubmissionResponse
        {
            Id = submission.Id,
            AssignmentId = submission.AssignmentId,
            AssignmentTitle = assignment?.Title ?? string.Empty,
            ClassId = classEntity?.Id ?? 0,
            ClassCode = classEntity?.ClassCode ?? string.Empty,
            StudentId = submission.StudentId,
            StudentName = student?.FullName ?? string.Empty,
            StudentEmail = student?.Email ?? string.Empty,
            FileId = submission.FileId,
            FileUrl = submission.File?.Url ?? string.Empty,
            Status = submission.Status,
            ContentJson = submission.ContentJson,
            AutoGradeResultJson = submission.AutoGradeResultJson,
            AutoScore = submission.AutoScore,
            FinalScore = submission.FinalScore,
            AttemptNumber = submission.AttemptNumber,
            Score = submission.Score,
            Feedback = submission.Feedback,
            GradedById = submission.GradedById,
            GradedByName = gradedBy?.FullName ?? string.Empty,
            GradedAt = submission.GradedAt,
            CreatedAt = submission.CreatedAt,
            UpdatedAt = submission.UpdatedAt
        };
    }

    public static SubmissionResponse MapWithAssignmentDetails(Submission submission)
    {
        var response = Map(submission);
        var assignment = submission.Assignment;

        if (assignment != null)
        {
            response.MaxScore = assignment.MaxScore;
            response.AssignmentType = assignment.AssignmentType;

            if (assignment.Rubric != null && !string.IsNullOrEmpty(assignment.Rubric.Criteria))
            {
                try
                {
                    var criteria = JsonSerializer.Deserialize<List<Application.Dtos.Assignments.RubricCriterionResponse>>(
                        assignment.Rubric.Criteria,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    response.RubricCriteria = criteria;
                }
                catch
                {
                }
            }

            // Add quiz details for review
            if (assignment.AssignmentType == "quiz" && assignment.QuizDetail != null)
            {
                try
                {
                    var quizDetail = assignment.QuizDetail;
                    var questions = new List<GradingQuizQuestion>();
                    if (!string.IsNullOrEmpty(quizDetail.QuestionsJson))
                    {
                        var parsedQuestions = JsonSerializer.Deserialize<List<JsonElement>>(quizDetail.QuestionsJson);
                        if (parsedQuestions != null)
                        {
                            foreach (var q in parsedQuestions)
                            {
                                var question = new GradingQuizQuestion
                                {
                                    Id = q.GetProperty("id").GetString() ?? "",
                                    Text = q.GetProperty("text").GetString() ?? "",
                                    Type = q.TryGetProperty("type", out var t) ? t.GetString() ?? "single_choice" : "single_choice"
                                };
                                if (q.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                                {
                                    question.Options = opts.EnumerateArray().Select(o => new GradingQuizOption
                                    {
                                        Id = o.GetProperty("id").GetString() ?? "",
                                        Text = o.GetProperty("text").GetString() ?? ""
                                    }).ToList();
                                }
                                questions.Add(question);
                            }
                        }
                    }
                    response.QuizDetail = new GradingQuizDetail
                    {
                        Questions = questions,
                        TimeLimitSeconds = quizDetail.TimeLimitSeconds,
                        ShuffleQuestions = quizDetail.ShuffleQuestions
                    };
                }
                catch
                {
                }
            }
        }

        return response;
    }
}
