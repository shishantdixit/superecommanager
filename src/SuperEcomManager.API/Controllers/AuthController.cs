using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Auth;
using SuperEcomManager.Infrastructure.RateLimiting;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Authentication endpoints for login, registration, and token management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public class AuthController : ApiControllerBase
{
    /// <summary>
    /// Authenticate user with email and password.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand
        {
            Email = request.Email,
            Password = request.Password,
            TenantSlug = request.TenantSlug
        };

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = result.Errors.FirstOrDefault() ?? "Authentication failed"
            });
        }

        return OkResponse(result.Value!, "Login successful");
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            TenantSlug = request.TenantSlug
        };

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = result.Errors.FirstOrDefault() ?? "Registration failed"
            });
        }

        return CreatedResponse("", result.Value!, "Registration successful");
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand
        {
            RefreshToken = request.RefreshToken,
            TenantSlug = request.TenantSlug
        };

        var result = await Mediator.Send(command);

        if (!result.IsSuccess)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = result.Errors.FirstOrDefault() ?? "Token refresh failed"
            });
        }

        return OkResponse(result.Value!, "Token refreshed successfully");
    }

    /// <summary>
    /// Get current authenticated user info.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<UserInfo>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst("name")?.Value ?? "";
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = User.FindAll("permission").Select(c => c.Value).ToList();

        var nameParts = name.Split(' ', 2);

        var userInfo = new UserInfo
        {
            Id = Guid.TryParse(userId, out var id) ? id : Guid.Empty,
            Email = email ?? "",
            FirstName = nameParts.Length > 0 ? nameParts[0] : "",
            LastName = nameParts.Length > 1 ? nameParts[1] : "",
            RoleName = roles.FirstOrDefault(),
            Permissions = permissions
        };

        return OkResponse(userInfo);
    }

    /// <summary>
    /// Logout user by revoking refresh token.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> Logout([FromBody] LogoutRequest request)
    {
        var command = new RevokeTokenCommand { RefreshToken = request.RefreshToken };
        await Mediator.Send(command);

        return OkResponse(true, "Logged out successfully");
    }
}

// Request DTOs
public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string TenantSlug { get; init; } = string.Empty;
}

public record RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string TenantSlug { get; init; } = string.Empty;
}

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
    public string TenantSlug { get; init; } = string.Empty;
}

public record LogoutRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
