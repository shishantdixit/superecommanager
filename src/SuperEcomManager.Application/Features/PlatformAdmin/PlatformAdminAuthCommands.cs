using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Domain.Entities.Platform;

namespace SuperEcomManager.Application.Features.PlatformAdmin;

/// <summary>
/// Command to authenticate a platform admin.
/// </summary>
public class PlatformAdminLoginCommand : IRequest<Result<PlatformAdminAuthResponse>>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}

public class PlatformAdminAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public PlatformAdminDto Admin { get; set; } = null!;
}

public class PlatformAdminLoginCommandHandler : IRequestHandler<PlatformAdminLoginCommand, Result<PlatformAdminAuthResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<PlatformAdminLoginCommandHandler> _logger;

    public PlatformAdminLoginCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<PlatformAdminLoginCommandHandler> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<PlatformAdminAuthResponse>> Handle(PlatformAdminLoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var admin = await _dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Email == normalizedEmail && a.DeletedAt == null, cancellationToken);

        if (admin == null)
        {
            _logger.LogWarning("Platform admin login failed: email not found {Email}", request.Email);
            return Result<PlatformAdminAuthResponse>.Failure("Invalid credentials");
        }

        if (!admin.IsActive)
        {
            _logger.LogWarning("Platform admin login failed: account inactive {Email}", request.Email);
            return Result<PlatformAdminAuthResponse>.Failure("Account is inactive");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, admin.PasswordHash))
        {
            admin.RecordFailedLogin();
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Platform admin login failed: invalid password {Email}", request.Email);
            return Result<PlatformAdminAuthResponse>.Failure("Invalid credentials");
        }

        // Record successful login
        admin.RecordLogin(request.IpAddress);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Generate tokens (platform admin uses a different token type)
        var token = _tokenService.GeneratePlatformAdminToken(admin);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token
        admin.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Platform admin logged in successfully: {Email}", request.Email);

        return Result<PlatformAdminAuthResponse>.Success(new PlatformAdminAuthResponse
        {
            AccessToken = token.Token,
            RefreshToken = refreshToken,
            ExpiresAt = token.ExpiresAt,
            Admin = new PlatformAdminDto
            {
                Id = admin.Id,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                IsSuperAdmin = admin.IsSuperAdmin,
                IsActive = admin.IsActive,
                LastLoginAt = admin.LastLoginAt,
                CreatedAt = admin.CreatedAt
            }
        });
    }
}

/// <summary>
/// Command to refresh platform admin token.
/// </summary>
public class PlatformAdminRefreshTokenCommand : IRequest<Result<PlatformAdminAuthResponse>>
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class PlatformAdminRefreshTokenCommandHandler : IRequestHandler<PlatformAdminRefreshTokenCommand, Result<PlatformAdminAuthResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<PlatformAdminRefreshTokenCommandHandler> _logger;

    public PlatformAdminRefreshTokenCommandHandler(
        IApplicationDbContext dbContext,
        ITokenService tokenService,
        ILogger<PlatformAdminRefreshTokenCommandHandler> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<PlatformAdminAuthResponse>> Handle(PlatformAdminRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a =>
                a.RefreshToken == request.RefreshToken &&
                a.RefreshTokenExpiresAt > DateTime.UtcNow &&
                a.DeletedAt == null &&
                a.IsActive, cancellationToken);

        if (admin == null)
        {
            return Result<PlatformAdminAuthResponse>.Failure("Invalid or expired refresh token");
        }

        // Generate new tokens
        var token = _tokenService.GeneratePlatformAdminToken(admin);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Update refresh token
        admin.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Platform admin token refreshed: {Email}", admin.Email);

        return Result<PlatformAdminAuthResponse>.Success(new PlatformAdminAuthResponse
        {
            AccessToken = token.Token,
            RefreshToken = refreshToken,
            ExpiresAt = token.ExpiresAt,
            Admin = new PlatformAdminDto
            {
                Id = admin.Id,
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                IsSuperAdmin = admin.IsSuperAdmin,
                IsActive = admin.IsActive,
                LastLoginAt = admin.LastLoginAt,
                CreatedAt = admin.CreatedAt
            }
        });
    }
}

/// <summary>
/// Command to logout platform admin (revoke refresh token).
/// </summary>
public class PlatformAdminLogoutCommand : IRequest<Result>
{
    public Guid AdminId { get; set; }
}

public class PlatformAdminLogoutCommandHandler : IRequestHandler<PlatformAdminLogoutCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<PlatformAdminLogoutCommandHandler> _logger;

    public PlatformAdminLogoutCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<PlatformAdminLogoutCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(PlatformAdminLogoutCommand request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Id == request.AdminId, cancellationToken);

        if (admin == null)
        {
            return Result.Failure("Admin not found");
        }

        admin.RevokeRefreshToken();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Platform admin logged out: {Email}", admin.Email);

        return Result.Success();
    }
}

/// <summary>
/// Command to change platform admin password.
/// </summary>
public class ChangePlatformAdminPasswordCommand : IRequest<Result>
{
    public Guid AdminId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePlatformAdminPasswordCommandHandler : IRequestHandler<ChangePlatformAdminPasswordCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ChangePlatformAdminPasswordCommandHandler> _logger;

    public ChangePlatformAdminPasswordCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<ChangePlatformAdminPasswordCommandHandler> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result> Handle(ChangePlatformAdminPasswordCommand request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.PlatformAdmins
            .FirstOrDefaultAsync(a => a.Id == request.AdminId && a.DeletedAt == null, cancellationToken);

        if (admin == null)
        {
            return Result.Failure("Admin not found");
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, admin.PasswordHash))
        {
            return Result.Failure("Current password is incorrect");
        }

        if (request.NewPassword.Length < 8)
        {
            return Result.Failure("New password must be at least 8 characters");
        }

        var hashedPassword = _passwordHasher.HashPassword(request.NewPassword);
        admin.UpdatePassword(hashedPassword);

        // Revoke refresh token to force re-login
        admin.RevokeRefreshToken();

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Platform admin password changed: {Email}", admin.Email);

        return Result.Success();
    }
}
