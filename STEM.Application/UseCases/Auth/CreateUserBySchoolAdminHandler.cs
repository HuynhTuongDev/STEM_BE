using Microsoft.Extensions.Configuration;
using STEM.Application.Dtos.Auth;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using FluentValidation;

namespace STEM.Application.UseCases.Auth;

public class CreateUserBySchoolAdminHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<CreateUserBySchoolAdminRequest> _validator;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public CreateUserBySchoolAdminHandler(
        IUserRepository userRepository,
        IValidator<CreateUserBySchoolAdminRequest> validator,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _validator = validator;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task Handle(
        int currentUserId,
        CreateUserBySchoolAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var trimmedEmail = request.Email.Trim();
        var trimmedPhone = request.Phone.Trim();

        var existingUser = await _userRepository.GetByEmailAsync(trimmedEmail, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException("Email is already registered.");

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("User is not authenticated properly.");

        if (currentUser.SchoolId == null)
            throw new UnauthorizedAccessException("School Admin must be associated with a school.");

        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        var user = new User
        {
            Email = trimmedEmail,
            FullName = request.FullName.Trim(),
            Phone = trimmedPhone,
            Gender = request.Gender,
            DateOfBirth = DateOnly.TryParse(request.DateOfBirth, out var dob) ? dob : (DateOnly?)null,
            Address = request.Address,
            IsActive = true,
            IsEmailVerified = true,
            RoleId = request.RoleId,
            SchoolId = currentUser.SchoolId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        await SendWelcomeEmailAsync(user, cancellationToken);
    }

    private async Task SendWelcomeEmailAsync(User user, CancellationToken cancellationToken)
    {
        var frontendBaseUrl = _configuration["AppSettings:FrontendBaseUrl"] ?? "https://localhost:3000";
        var roleName = user.RoleId == 3 ? "Giáo viên" : "Học sinh";

        var body = $@"
            <h2>Chào mừng đến với STEM</h2>
            <p>Xin chào <strong>{user.FullName}</strong>,</p>
            <p>Tài khoản <strong>{roleName}</strong> của bạn đã được tạo bởi Quản trị viên trường.</p>
            <p>Bạn có thể đăng nhập bằng Google với email <strong>{user.Email}</strong>.</p>
            <p><a href='{frontendBaseUrl}/login' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Đăng nhập ngay</a></p>
        ";
        await _emailService.SendEmailAsync(user.Email, $"Chào mừng đến với STEM - Tài khoản {roleName}", body, cancellationToken);
    }
}
