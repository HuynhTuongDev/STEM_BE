namespace STEM.Application.Dtos.Students;

public class BulkCreateStudentsRequest
{
    public List<CreateStudentRequest> Students { get; set; } = new();
}

public class BulkCreateStudentResult
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
