using FluentValidation;
using STEM.Application.Dtos.Students;

namespace STEM.Application.Validators;

public class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.FullName));

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
