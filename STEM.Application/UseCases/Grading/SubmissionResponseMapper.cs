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
            Score = submission.Score,
            Feedback = submission.Feedback,
            GradedById = submission.GradedById,
            GradedByName = gradedBy?.FullName ?? string.Empty,
            GradedAt = submission.GradedAt,
            CreatedAt = submission.CreatedAt,
            UpdatedAt = submission.UpdatedAt
        };
    }
}
