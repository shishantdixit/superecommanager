using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Identity;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Auth;

/// <summary>
/// Handles user registration.
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _applicationDb;
    private readonly ITenantDbContext _tenantDb;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IApplicationDbContext applicationDb,
        ITenantDbContext tenantDb,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ICurrentTenantService currentTenantService,
        ILogger<RegisterCommandHandler> logger)
    {
        _applicationDb = applicationDb;
        _tenantDb = tenantDb;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentTenantService = currentTenantService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Find tenant
        var tenant = await _applicationDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == request.TenantSlug && t.Status == TenantStatus.Active, cancellationToken);

        if (tenant == null)
        {
            _logger.LogWarning("Registration attempt for non-existent tenant: {TenantSlug}", request.TenantSlug);
            return Result<AuthResponse>.Failure("Invalid tenant");
        }

        // Set tenant context
        _currentTenantService.SetTenant(tenant.Id, tenant.SchemaName, tenant.Slug);

        // Check if email already exists
        var emailLower = request.Email.ToLowerInvariant();
        var existingUser = await _tenantDb.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == emailLower, cancellationToken);

        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email} in tenant {TenantSlug}",
                request.Email, request.TenantSlug);
            return Result<AuthResponse>.Failure("Email already registered");
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user
        var user = User.Create(
            email: request.Email,
            firstName: request.FirstName,
            lastName: request.LastName,
            passwordHash: passwordHash,
            phone: request.Phone
        );

        // Assign default role (Viewer)
        var viewerRole = await _tenantDb.Roles
            .FirstOrDefaultAsync(r => r.Name == "Viewer" && r.IsSystem, cancellationToken);

        if (viewerRole != null)
        {
            user.AssignRole(viewerRole.Id);
        }

        await _tenantDb.Users.AddAsync(user, cancellationToken);
        await _tenantDb.SaveChangesAsync(cancellationToken);

        // Load user with roles for token generation
        var createdUser = await _tenantDb.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);

        // Get permissions
        var permissions = createdUser.UserRoles
            .SelectMany(ur => ur.Role?.RolePermissions ?? Enumerable.Empty<RolePermission>())
            .Select(rp => rp.Permission?.Code)
            .Where(p => p != null)
            .Distinct()
            .Cast<string>()
            .ToList();

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(createdUser, tenant.Id, tenant.Slug, permissions);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _tokenService.StoreRefreshTokenAsync(createdUser.Id, tenant.Id, refreshToken, refreshTokenExpiry, cancellationToken);

        _logger.LogInformation("User {Email} registered successfully for tenant {TenantSlug}",
            request.Email, request.TenantSlug);

        return Result<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserInfo
            {
                Id = createdUser.Id,
                Email = createdUser.Email,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                RoleName = viewerRole?.Name,
                Permissions = permissions
            }
        });
    }
}
