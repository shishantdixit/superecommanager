using FluentValidation;

namespace SuperEcomManager.Application.Features.Auth;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters")
            .Matches(@"^[\d\s\-\+\(\)]*$").WithMessage("Phone contains invalid characters")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.TenantSlug)
            .NotEmpty().WithMessage("Tenant is required");
    }
}
