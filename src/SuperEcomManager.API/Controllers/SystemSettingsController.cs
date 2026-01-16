using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Features.PlatformAdmin;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for platform system settings management (Platform Admin).
/// </summary>
[ApiController]
[Route("api/platform-admin/settings")]
[Authorize(Policy = "PlatformAdmin")]
public class SystemSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all platform settings grouped by category.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SettingsByCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings([FromQuery] string? category)
    {
        var result = await _mediator.Send(new GetPlatformSettingsQuery
        {
            Category = category,
            PublicOnly = false
        });

        return Ok(result);
    }

    /// <summary>
    /// Get public settings only (no auth required).
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<SettingsByCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicSettings()
    {
        var result = await _mediator.Send(new GetPlatformSettingsQuery
        {
            PublicOnly = true
        });

        return Ok(result);
    }

    /// <summary>
    /// Get setting categories.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _mediator.Send(new GetSettingCategoriesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get a specific setting by key.
    /// </summary>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(PlatformSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSetting(string key)
    {
        var result = await _mediator.Send(new GetPlatformSettingByKeyQuery { Key = key });

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Create or update a platform setting.
    /// </summary>
    [HttpPut("{key}")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(typeof(PlatformSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertSetting(string key, [FromBody] UpsertSettingRequest request)
    {
        var result = await _mediator.Send(new UpsertPlatformSettingCommand
        {
            Key = key,
            Value = request.Value,
            Category = request.Category,
            Description = request.Description,
            IsPublic = request.IsPublic,
            IsEncrypted = request.IsEncrypted
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update multiple settings at once.
    /// </summary>
    [HttpPut]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateBulkSettings([FromBody] UpdateBulkSettingsRequest request)
    {
        var result = await _mediator.Send(new UpdateBulkSettingsCommand
        {
            Settings = request.Settings.Select(s => new SettingUpdateItem
            {
                Key = s.Key,
                Value = s.Value
            }).ToList()
        });

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = $"Updated {request.Settings.Count} settings" });
    }

    /// <summary>
    /// Delete a platform setting.
    /// </summary>
    [HttpDelete("{key}")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSetting(string key)
    {
        var result = await _mediator.Send(new DeletePlatformSettingCommand { Key = key });

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Setting deleted successfully" });
    }

    /// <summary>
    /// Seed default settings.
    /// </summary>
    [HttpPost("seed-defaults")]
    [Authorize(Policy = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedDefaults()
    {
        var result = await _mediator.Send(new SeedDefaultSettingsCommand());

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Errors.FirstOrDefault() });
        }

        return Ok(new { message = "Default settings seeded successfully" });
    }
}

#region Request DTOs

public record UpsertSettingRequest(
    string Value,
    string Category,
    string? Description = null,
    bool IsPublic = false,
    bool IsEncrypted = false);

public record UpdateBulkSettingsRequest(List<SettingUpdateRequest> Settings);

public record SettingUpdateRequest(string Key, string Value);

#endregion
