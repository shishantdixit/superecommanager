using MediatR;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Auth;

/// <summary>
/// Command to register a new user in a tenant.
/// </summary>
public record RegisterCommand : IRequest<Result<AuthResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string TenantSlug { get; init; } = string.Empty;
}
