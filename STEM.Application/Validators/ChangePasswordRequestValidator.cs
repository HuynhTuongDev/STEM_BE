using FluentValidation;
using STEM.Application.Dtos.Auth;

namespace STEM.Application.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("Old password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("New password must be at least 6 characters.")
            .Matches("[A-Z]").WithMessage("New password must contain at least 1 uppercase letter.")
            .Matches("[0-9]").WithMessage("New password must contain at least 1 digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("New password must contain at least 1 special character.")
            .NotEqual(x => x.OldPassword).WithMessage("New password must be different from the old password.");
    }
}
