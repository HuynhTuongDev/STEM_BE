using STEM.Application.Dtos.Grading;
using STEM.Core.Entities.Projects;

namespace STEM.Application.UseCases.Grading;

internal static class ResubmitRequestResponseMapper
{
    public static ResubmitRequestResponse Map(ResubmitRequest request)
    {
        var classEntity = request.Assignment?.Class;

        return new ResubmitRequestResponse
        {
            Id = request.Id,
            AssignmentId = request.AssignmentId,
            AssignmentTitle = request.Assignment?.Title ?? string.Empty,
            ClassId = classEntity?.Id ?? 0,
            ClassCode = classEntity?.ClassCode ?? string.Empty,
            StudentId = request.StudentId,
            StudentName = request.Student?.FullName ?? string.Empty,
            Reason = request.Reason,
            Status = request.Status,
            GrantedExtraAttempts = request.GrantedExtraAttempts,
            GrantedNewDueDate = request.GrantedNewDueDate,
            ReviewNote = request.ReviewNote,
            ReviewedById = request.ReviewedById,
            ReviewedByName = request.ReviewedBy?.FullName ?? string.Empty,
            ReviewedAt = request.ReviewedAt,
            CreatedAt = request.CreatedAt
        };
    }
}
