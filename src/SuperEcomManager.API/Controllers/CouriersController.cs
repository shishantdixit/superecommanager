using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Couriers;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Courier account management endpoints.
/// </summary>
[Authorize]
public class CouriersController : ApiControllerBase
{
    /// <summary>
    /// Get all courier accounts.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CourierAccountDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CourierAccountDto>>>> GetCourierAccounts(
        [FromQuery] bool? isActive = null)
    {
        var accounts = await Mediator.Send(new GetCourierAccountsQuery { IsActive = isActive });
        return OkResponse(accounts);
    }

    /// <summary>
    /// Get available courier types.
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableCourierDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<AvailableCourierDto>>> GetAvailableCouriers()
    {
        var couriers = new List<AvailableCourierDto>
        {
            new()
            {
                CourierType = CourierType.Shiprocket,
                Name = "Shiprocket",
                Description = "Aggregator with multiple courier partners",
                IsAggregator = true,
                RequiresApiKey = true,
                RequiresApiSecret = false,
                RequiresAccessToken = true,
                Features = new List<string> { "Auto Courier Selection", "NDR Management", "COD", "Reverse Pickup" }
            },
            new()
            {
                CourierType = CourierType.Delhivery,
                Name = "Delhivery",
                Description = "Direct integration with Delhivery",
                IsAggregator = false,
                RequiresApiKey = true,
                RequiresApiSecret = false,
                RequiresAccessToken = true,
                Features = new List<string> { "Express Delivery", "COD", "Surface" }
            },
            new()
            {
                CourierType = CourierType.BlueDart,
                Name = "BlueDart",
                Description = "Premium courier service",
                IsAggregator = false,
                RequiresApiKey = true,
                RequiresApiSecret = true,
                RequiresAccessToken = false,
                Features = new List<string> { "Express Delivery", "Time Definite", "COD" }
            },
            new()
            {
                CourierType = CourierType.DTDC,
                Name = "DTDC",
                Description = "Pan-India courier network",
                IsAggregator = false,
                RequiresApiKey = true,
                RequiresApiSecret = true,
                RequiresAccessToken = false,
                Features = new List<string> { "Express", "Lite", "COD" }
            },
            new()
            {
                CourierType = CourierType.EcomExpress,
                Name = "Ecom Express",
                Description = "E-commerce focused courier",
                IsAggregator = false,
                RequiresApiKey = true,
                RequiresApiSecret = true,
                RequiresAccessToken = false,
                Features = new List<string> { "COD", "Reverse Pickup", "NDR" }
            },
            new()
            {
                CourierType = CourierType.XpressBees,
                Name = "XpressBees",
                Description = "Technology-driven logistics",
                IsAggregator = false,
                RequiresApiKey = true,
                RequiresApiSecret = false,
                RequiresAccessToken = true,
                Features = new List<string> { "Express", "COD", "Reverse" }
            },
            new()
            {
                CourierType = CourierType.Shadowfax,
                Name = "Shadowfax",
                Description = "Hyperlocal and express delivery",
                IsAggregator = false,
                RequiresApiKey = true,
                RequiresApiSecret = false,
                RequiresAccessToken = true,
                Features = new List<string> { "Same Day", "Express", "COD" }
            },
            new()
            {
                CourierType = CourierType.Custom,
                Name = "Custom",
                Description = "Custom courier integration",
                IsAggregator = false,
                RequiresApiKey = true,
                RequiresApiSecret = true,
                RequiresAccessToken = false,
                Features = new List<string> { "Custom API" }
            }
        };

        return OkResponse(couriers);
    }

    /// <summary>
    /// Create a new courier account.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CourierAccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CourierAccountDto>>> CreateCourierAccount(
        [FromBody] CreateCourierAccountRequest request)
    {
        var result = await Mediator.Send(new CreateCourierAccountCommand
        {
            Name = request.Name,
            CourierType = request.CourierType,
            IsDefault = request.IsDefault,
            Priority = request.Priority
        });

        if (!result.IsSuccess)
            return BadRequestResponse<CourierAccountDto>(result.Errors.FirstOrDefault() ?? "Failed to create account");

        return CreatedResponse($"/api/couriers/{result.Value!.Id}", result.Value);
    }

    /// <summary>
    /// Update courier account credentials.
    /// </summary>
    [HttpPut("{id:guid}/credentials")]
    [ProducesResponseType(typeof(ApiResponse<CourierAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourierAccountDto>>> UpdateCredentials(
        Guid id,
        [FromBody] UpdateCourierCredentialsRequest request)
    {
        var result = await Mediator.Send(new UpdateCourierCredentialsCommand
        {
            AccountId = id,
            ApiKey = request.ApiKey,
            ApiSecret = request.ApiSecret,
            AccessToken = request.AccessToken,
            AccountId_ = request.AccountId,
            ChannelId = request.ChannelId
        });

        if (!result.IsSuccess)
            return NotFoundResponse<CourierAccountDto>(result.Errors.FirstOrDefault() ?? "Account not found");

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Set a courier account as the default.
    /// </summary>
    [HttpPost("{id:guid}/set-default")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetAsDefault(Guid id)
    {
        var result = await Mediator.Send(new SetDefaultCourierCommand { AccountId = id });

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Errors.FirstOrDefault() ?? "Failed to set default"));

        return Ok(ApiResponse<object>.Ok(null, "Default courier updated"));
    }

    /// <summary>
    /// Activate a courier account.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Activate(Guid id)
    {
        var result = await Mediator.Send(new ActivateCourierCommand { AccountId = id });

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Errors.FirstOrDefault() ?? "Failed to activate"));

        return Ok(ApiResponse<object>.Ok(null, "Courier account activated"));
    }

    /// <summary>
    /// Deactivate a courier account.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await Mediator.Send(new DeactivateCourierCommand { AccountId = id });

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Errors.FirstOrDefault() ?? "Failed to deactivate"));

        return Ok(ApiResponse<object>.Ok(null, "Courier account deactivated"));
    }

    /// <summary>
    /// Delete a courier account.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await Mediator.Send(new DeleteCourierAccountCommand { AccountId = id });

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Errors.FirstOrDefault() ?? "Failed to delete"));

        return NoContent();
    }

    /// <summary>
    /// Test courier account connection.
    /// </summary>
    [HttpPost("{id:guid}/test")]
    [ProducesResponseType(typeof(ApiResponse<CourierConnectionTestResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CourierConnectionTestResult>>> TestConnection(Guid id)
    {
        var result = await Mediator.Send(new TestCourierConnectionCommand { AccountId = id });

        if (!result.IsSuccess)
            return BadRequestResponse<CourierConnectionTestResult>(result.Errors.FirstOrDefault() ?? "Connection test failed");

        return OkResponse(result.Value!);
    }
}
