namespace STEM.Application.Dtos.Students;

public class StudentsListResponse
{
    public int Total { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyCollection<StudentResponse> Data { get; set; } = Array.Empty<StudentResponse>();
}
