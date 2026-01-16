using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Features.PlatformAdmin;
using System.Security.Claims;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for platform admin authentication.
/// </summary>
[ApiController]
[Route("api/platform-admin/auth")]
public class PlatformAdminAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformAdminAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Login as a platform admin.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PlatformAdminAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] PlatformAdminLoginRequest request)
    {
        var command = new PlatformAdminLoginCommand
        {
            Email = request.Email,
            Password = request.Password,
            IpAddress = GetClientIpAddress()
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Refresh access token.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PlatformAdminAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] PlatformAdminRefreshTokenRequest request)
    {
        var command = new PlatformAdminRefreshTokenCommand
        {
            RefreshToken = request.RefreshToken
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Logout (revoke refresh token).
    /// </summary>
    [HttpPost("logout")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var adminId = GetCurrentAdminId();
        if (adminId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new PlatformAdminLogoutCommand { AdminId = adminId.Value });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Change password.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] PlatformAdminChangePasswordRequest request)
    {
        var adminId = GetCurrentAdminId();
        if (adminId == null)
        {
            return Unauthorized();
        }

        var command = new ChangePlatformAdminPasswordCommand
        {
            AdminId = adminId.Value,
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Password changed successfully. Please login again." });
    }

    /// <summary>
    /// Get current admin profile.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = "PlatformAdmin")]
    [ProducesResponseType(typeof(PlatformAdminDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentProfile()
    {
        var adminId = GetCurrentAdminId();
        if (adminId == null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetPlatformAdminByIdQuery { AdminId = adminId.Value });

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    private string? GetClientIpAddress()
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private Guid? GetCurrentAdminId()
    {
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(adminIdClaim, out var adminId))
        {
            return adminId;
        }
        return null;
    }
}

#region Request DTOs

public record PlatformAdminLoginRequest(string Email, string Password);
public record PlatformAdminRefreshTokenRequest(string RefreshToken);
public record PlatformAdminChangePasswordRequest(string CurrentPassword, string NewPassword);

#endregion
