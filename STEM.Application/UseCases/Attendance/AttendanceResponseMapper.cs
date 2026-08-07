using STEM.Application.Dtos.Attendance;
using STEM.Core.Entities.Classes;

namespace STEM.Application.UseCases.Attendance;

internal static class AttendanceResponseMapper
{
    public static AttendanceResponse Map(AttendanceRecord record)
    {
        return new AttendanceResponse
        {
            Id = record.Id,
            ClassId = record.ClassId,
            ClassCode = record.Class?.ClassCode ?? string.Empty,
            StudentId = record.StudentId,
            StudentName = record.Student?.FullName ?? string.Empty,
            StudentEmail = record.Student?.Email ?? string.Empty,
            AttendanceDate = record.AttendanceDate,
            Status = record.Status,
            Note = record.Note,
            MarkedById = record.MarkedById,
            MarkedByName = record.MarkedBy?.FullName ?? string.Empty,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}
