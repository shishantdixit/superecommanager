using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Identity;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Auth;

/// <summary>
/// Handles user login authentication.
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _applicationDb;
    private readonly ITenantDbContext _tenantDb;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IApplicationDbContext applicationDb,
        ITenantDbContext tenantDb,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ICurrentTenantService currentTenantService,
        ILogger<LoginCommandHandler> logger)
    {
        _applicationDb = applicationDb;
        _tenantDb = tenantDb;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find tenant
        var tenant = await _applicationDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == request.TenantSlug && t.Status == TenantStatus.Active, cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Login attempt for non-existent tenant: {TenantSlug}", request.TenantSlug);
            return Result<AuthResponse>.Failure("Invalid credentials");
        }

        // Set tenant context for tenant-specific queries
        _currentTenantService.SetTenant(tenant.Id, tenant.SchemaName, tenant.Slug);

        // Find user with roles and permissions
        var user = await _tenantDb.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant() && u.IsActive, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Email} in tenant {TenantSlug}",
                request.Email, request.TenantSlug);
            return Result<AuthResponse>.Failure("Invalid credentials");
        }

        // Check if locked out
        if (user.IsLockedOut())
        {
            _logger.LogWarning("Login attempt for locked out user: {Email} in tenant {TenantSlug}",
                request.Email, request.TenantSlug);
            return Result<AuthResponse>.Failure("Account is temporarily locked. Please try again later.");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await _tenantDb.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Failed login attempt for user: {Email} in tenant {TenantSlug}",
                request.Email, request.TenantSlug);
            return Result<AuthResponse>.Failure("Invalid credentials");
        }

        // Get user permissions from all roles
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role?.RolePermissions ?? Enumerable.Empty<RolePermission>())
            .Select(rp => rp.Permission?.Code)
            .Where(p => p != null)
            .Distinct()
            .Cast<string>()
            .ToList();

        // Get primary role name (first role)
        var primaryRole = user.UserRoles.FirstOrDefault()?.Role;

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user, tenant.Id, tenant.Slug, permissions);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token (expires in 7 days by default)
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _tokenService.StoreRefreshTokenAsync(user.Id, tenant.Id, refreshToken, refreshTokenExpiry, cancellationToken);

        // Record successful login
        user.RecordLogin();
        await _tenantDb.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {Email} logged in successfully for tenant {TenantSlug}",
            request.Email, request.TenantSlug);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
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
