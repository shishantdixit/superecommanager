using MediatR;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Auth;

/// <summary>
/// Command to authenticate a user with email and password.
/// </summary>
public record LoginCommand : IRequest<Result<AuthResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string TenantSlug { get; init; } = string.Empty;
}
