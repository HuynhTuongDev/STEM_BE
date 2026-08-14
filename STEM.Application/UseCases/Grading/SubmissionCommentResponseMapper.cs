using STEM.Application.Dtos.Grading;
using STEM.Core.Entities.Projects;

namespace STEM.Application.UseCases.Grading;

internal static class SubmissionCommentResponseMapper
{
    public static SubmissionCommentResponse Map(SubmissionComment comment)
    {
        return new SubmissionCommentResponse
        {
            Id = comment.Id,
            SubmissionId = comment.SubmissionId,
            AuthorId = comment.AuthorId,
            AuthorName = comment.Author?.FullName ?? string.Empty,
            AuthorRole = comment.Author?.Role?.Name ?? string.Empty,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}
