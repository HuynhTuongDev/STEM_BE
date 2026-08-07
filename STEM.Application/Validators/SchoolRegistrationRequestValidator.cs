using FluentValidation;
using STEM.Application.Dtos.Schools;

namespace STEM.Application.Validators;

public class SchoolRegistrationRequestValidator : AbstractValidator<SchoolRegistrationRequest>
{
    public SchoolRegistrationRequestValidator()
    {
        // School validation
        RuleFor(x => x.SchoolName)
            .NotEmpty().WithMessage("School name is required.");

        RuleFor(x => x.SchoolAddress)
            .NotEmpty().WithMessage("School address is required.");

        RuleFor(x => x.RepresentativeName)
            .NotEmpty().WithMessage("Representative name is required.");

        RuleFor(x => x.RepresentativeEmail)
            .NotEmpty().WithMessage("Representative email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        // User validation
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone number must be exactly 10 digits.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least 1 uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least 1 digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least 1 special character.");
    }
}
