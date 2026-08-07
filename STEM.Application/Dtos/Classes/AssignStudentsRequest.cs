using STEM.Core.Repository;

namespace STEM.Application.Dtos.Classes;

public class AssignStudentsRequest
{
    public List<int> StudentIds { get; set; } = new();
    public bool StrictMode { get; set; } = false;
}

public class AssignStudentsResponse
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int AlreadyEnrolledCount { get; set; }
    public int ConflictCount { get; set; }
    public List<int> AlreadyEnrolledStudentIds { get; set; } = new();
    public List<StudentResponse> AddedStudents { get; set; } = new();
    public List<StudentScheduleConflict> ConflictingStudents { get; set; } = new();
}
