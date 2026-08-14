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
        }

        return response;
    }
}
