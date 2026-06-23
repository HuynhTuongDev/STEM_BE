using FluentValidation;
using STEM.Application.Dtos.Auth;

namespace STEM.Application.Validators;

public class CreateUserBySchoolAdminValidator : AbstractValidator<CreateUserBySchoolAdminRequest>
{
    public CreateUserBySchoolAdminValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone number must be exactly 10 digits.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role is required.")
            .Must(roleId => roleId == 3 || roleId == 4)
            .WithMessage("Only Teacher (3) or Student (4) roles can be created by School Admin.");
    }
}
