using FluentValidation;
using STEM.Application.Dtos.Auth;

namespace STEM.Application.Validators;

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.IdToken) || !string.IsNullOrWhiteSpace(x.Credential))
            .WithMessage("Google id token is required.");
    }
}
