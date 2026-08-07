using FluentValidation;
using STEM.Application.Dtos.Auth;
using STEM.Core.Repository;

namespace STEM.Application.Validators;

public class CreateUserBySchoolAdminValidator : AbstractValidator<CreateUserBySchoolAdminRequest>
{
    private readonly IUserRepository _userRepository;

    public CreateUserBySchoolAdminValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MinimumLength(2).WithMessage("Họ tên phải có ít nhất 2 ký tự.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .MustAsync(BeUniqueEmailAsync).WithMessage("Email đã được sử dụng.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .Matches(@"^\d{10}$").WithMessage("Số điện thoại phải có đúng 10 chữ số.")
            .MustAsync(BeUniquePhoneAsync).WithMessage("Số điện thoại đã được sử dụng.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Giới tính không được để trống.")
            .Must(g => g == "Male" || g == "Female" || g == "Other")
            .WithMessage("Giới tính phải là Male, Female hoặc Other.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Ngày sinh không được để trống.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Địa chỉ không được để trống.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role is required.")
            .Must(roleId => roleId == 3 || roleId == 4)
            .WithMessage("Only Teacher (3) or Student (4) roles can be created by School Admin.");
    }

    private async Task<bool> BeUniqueEmailAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var trimmedEmail = email.Trim();
        var existing = await _userRepository.GetByEmailAsync(trimmedEmail, cancellationToken);
        return existing == null;
    }

    private async Task<bool> BeUniquePhoneAsync(string phone, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        var trimmedPhone = phone.Trim();
        var existing = await _userRepository.GetByPhoneAsync(trimmedPhone, cancellationToken);
        return existing == null;
    }
}
