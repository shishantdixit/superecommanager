using MediatR;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Auth;

/// <summary>
/// Command to refresh access token using refresh token.
/// </summary>
public record RefreshTokenCommand : IRequest<Result<AuthResponse>>
{
    public string RefreshToken { get; init; } = string.Empty;
    public string TenantSlug { get; init; } = string.Empty;
}
