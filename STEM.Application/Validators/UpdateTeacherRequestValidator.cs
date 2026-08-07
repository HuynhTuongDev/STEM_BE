using FluentValidation;
using STEM.Application.Dtos.Users;

namespace STEM.Application.Validators;

public class UpdateTeacherRequestValidator : AbstractValidator<UpdateTeacherRequest>
{
    public UpdateTeacherRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Teacher ID is required.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .Matches(@"^\d{10}$").WithMessage("Phone number must be exactly 10 digits.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Gender)
            .MaximumLength(10).WithMessage("Gender cannot exceed 10 characters.")
            .When(x => !string.IsNullOrEmpty(x.Gender));

        RuleFor(x => x.Address)
            .MaximumLength(255).WithMessage("Address cannot exceed 255 characters.")
            .When(x => !string.IsNullOrEmpty(x.Address));
    }
}
