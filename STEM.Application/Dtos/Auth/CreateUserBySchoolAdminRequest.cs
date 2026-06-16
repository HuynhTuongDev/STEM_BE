namespace STEM.Application.Dtos.Auth;

public class CreateUserBySchoolAdminRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int RoleId { get; set; }  // 3 = Teacher, 4 = Student
}
