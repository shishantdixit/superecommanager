using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Identity;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Auth;

/// <summary>
/// Handles refresh token rotation.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _applicationDb;
    private readonly ITenantDbContext _tenantDb;
    private readonly ITokenService _tokenService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IApplicationDbContext applicationDb,
        ITenantDbContext tenantDb,
        ITokenService tokenService,
        ICurrentTenantService currentTenantService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _applicationDb = applicationDb;
        _tenantDb = tenantDb;
        _tokenService = tokenService;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find tenant
        var tenant = await _applicationDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == request.TenantSlug && t.Status == TenantStatus.Active, cancellationToken);

        if (tenant == null)
        {
            return Result<AuthResponse>.Failure("Invalid tenant");
        }

        // Set tenant context
        _currentTenantService.SetTenant(tenant.Id, tenant.SchemaName, tenant.Slug);

        // Validate refresh token
        var userId = await _tokenService.ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (!userId.HasValue)
        {
            _logger.LogWarning("Invalid refresh token attempt for tenant {TenantSlug}", request.TenantSlug);
            return Result<AuthResponse>.Failure("Invalid or expired refresh token");
        }

        // Get user with roles and permissions
        var user = await _tenantDb.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Refresh token for non-existent or inactive user: {UserId}", userId.Value);
            await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
            return Result<AuthResponse>.Failure("Invalid refresh token");
        }

        // Revoke old refresh token
        await _tokenService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);

        // Get permissions
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role?.RolePermissions ?? Enumerable.Empty<RolePermission>())
            .Select(rp => rp.Permission?.Code)
            .Where(p => p != null)
            .Distinct()
            .Cast<string>()
            .ToList();

        // Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(user, tenant.Id, tenant.Slug, permissions);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // Store new refresh token
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _tokenService.StoreRefreshTokenAsync(user.Id, tenant.Id, newRefreshToken, refreshTokenExpiry, cancellationToken);

        var primaryRole = user.UserRoles.FirstOrDefault()?.Role;

        _logger.LogInformation("Tokens refreshed for user {Email} in tenant {TenantSlug}",
            user.Email, request.TenantSlug);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleName = primaryRole?.Name,
                Permissions = permissions
            }
        });
    }
}
