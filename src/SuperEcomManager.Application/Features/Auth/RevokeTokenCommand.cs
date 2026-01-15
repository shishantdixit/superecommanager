using MediatR;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;

namespace SuperEcomManager.Application.Features.Auth;

/// <summary>
/// Command to revoke a refresh token (logout).
/// </summary>
public record RevokeTokenCommand : IRequest<Result<bool>>
{
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Handler for revoking refresh tokens.
/// </summary>
public class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result<bool>>
{
    private readonly ITokenService _tokenService;

    public RevokeTokenCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<Result<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        return Result<bool>.Success(true);
    }
}
