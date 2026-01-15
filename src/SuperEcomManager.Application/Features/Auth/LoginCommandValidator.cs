using FluentValidation;

namespace SuperEcomManager.Application.Features.Auth;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");

        RuleFor(x => x.TenantSlug)
            .NotEmpty().WithMessage("Tenant is required");
    }
}
