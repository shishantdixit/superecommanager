using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Couriers;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Courier account management endpoints.
/// </summary>
[Authorize]
public class CourierAccountsController : ApiControllerBase
{

    /// <summary>
    /// Get all courier accounts.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CourierAccountDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CourierAccountDto>>>> GetCourierAccounts(
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isConnected = null)
    {
        var query = new GetCourierAccountsQuery
        {
            IsActive = isActive,
            IsConnected = isConnected
        };
        var accounts = await Mediator.Send(query);

        return OkResponse(accounts, "Courier accounts retrieved successfully.");
    }

    /// <summary>
    /// Get courier account by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CourierAccountDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CourierAccountDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourierAccountDetailDto>>> GetCourierAccount(Guid id)
    {
        var query = new GetCourierAccountByIdQuery { Id = id };
        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return NotFoundResponse<CourierAccountDetailDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value, "Courier account retrieved successfully.");
    }

    /// <summary>
    /// Create a new courier account.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CourierAccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CourierAccountDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CourierAccountDto>>> CreateCourierAccount(
        [FromBody] CreateCourierAccountApiRequest request)
    {
        // Step 1: Create the account
        var createCommand = new CreateCourierAccountCommand
        {
            Name = request.Name,
            CourierType = request.CourierType,
            IsDefault = request.IsDefault,
            Priority = request.Priority ?? 100
        };

        var createResult = await Mediator.Send(createCommand);

        if (createResult.IsFailure)
            return BadRequestResponse<CourierAccountDto>(string.Join(", ", createResult.Errors));

        // Step 2: Update credentials if provided
        if (!string.IsNullOrEmpty(request.ApiKey) || !string.IsNullOrEmpty(request.ApiSecret))
        {
            var credsCommand = new UpdateCourierCredentialsCommand
            {
                AccountId = createResult.Value!.Id,
                ApiKey = request.ApiKey,
                ApiSecret = request.ApiSecret,
                AccessToken = request.AccessToken,
                AccountId_ = request.AccountId,
                ChannelId = request.ChannelId
            };

            var credsResult = await Mediator.Send(credsCommand);

            if (credsResult.IsSuccess)
            {
                return CreatedAtAction(
                    nameof(GetCourierAccount),
                    new { id = credsResult.Value!.Id },
                    ApiResponse<CourierAccountDto>.Ok(credsResult.Value, "Courier account created and connected successfully."));
            }
        }

        return CreatedAtAction(
            nameof(GetCourierAccount),
            new { id = createResult.Value!.Id },
            ApiResponse<CourierAccountDto>.Ok(createResult.Value, "Courier account created successfully."));
    }

    /// <summary>
    /// Update courier account credentials.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CourierAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CourierAccountDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CourierAccountDto>>> UpdateCourierAccount(
        Guid id,
        [FromBody] UpdateCourierAccountRequest request)
    {
        var command = new UpdateCourierAccountCommand
        {
            Id = id,
            Name = request.Name,
            ApiKey = request.ApiKey,
            ApiSecret = request.ApiSecret,
            AccountId = request.AccountId,
            ChannelId = request.ChannelId,
            PickupLocation = request.PickupLocation,
            IsActive = request.IsActive
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure)
            return BadRequestResponse<CourierAccountDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value, "Courier account updated successfully.");
    }

    /// <summary>
    /// Delete courier account.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCourierAccount(Guid id)
    {
        var command = new DeleteCourierAccountCommand { AccountId = id };
        var result = await Mediator.Send(command);

        if (result.IsFailure)
            return NotFoundResponse<bool>(string.Join(", ", result.Errors));

        return OkResponse(result.Value, "Courier account deleted successfully.");
    }

    /// <summary>
    /// Test courier account connection.
    /// </summary>
    [HttpPost("{id:guid}/test")]
    [ProducesResponseType(typeof(ApiResponse<CourierConnectionTestResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CourierConnectionTestResult>>> TestConnection(Guid id)
    {
        var command = new TestCourierConnectionCommand { AccountId = id };
        var result = await Mediator.Send(command);

        if (result.IsFailure)
            return BadRequestResponse<CourierConnectionTestResult>(string.Join(", ", result.Errors));

        return OkResponse(result.Value, "Connection test completed.");
    }

    /// <summary>
    /// Get Shiprocket channels for a courier account.
    /// </summary>
    [HttpGet("{id:guid}/shiprocket-channels")]
    [ProducesResponseType(typeof(ApiResponse<List<ShiprocketChannelDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ShiprocketChannelDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<ShiprocketChannelDto>>>> GetShiprocketChannels(Guid id)
    {
        var query = new GetShiprocketChannelsQuery { CourierAccountId = id };
        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return BadRequestResponse<List<ShiprocketChannelDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value, "Shiprocket channels retrieved successfully.");
    }

    /// <summary>
    /// Get Shiprocket pickup locations for a courier account.
    /// </summary>
    [HttpGet("{id:guid}/shiprocket-pickup-locations")]
    [ProducesResponseType(typeof(ApiResponse<List<ShiprocketPickupLocationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ShiprocketPickupLocationDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<ShiprocketPickupLocationDto>>>> GetShiprocketPickupLocations(Guid id)
    {
        var query = new GetShiprocketPickupLocationsQuery { CourierAccountId = id };
        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return BadRequestResponse<List<ShiprocketPickupLocationDto>>(string.Join(", ", result.Errors));

        return OkResponse(result.Value, "Shiprocket pickup locations retrieved successfully.");
    }

    /// <summary>
    /// Get wallet balance for courier account.
    /// </summary>
    [HttpGet("{id:guid}/wallet-balance")]
    [ProducesResponseType(typeof(ApiResponse<CourierWalletBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CourierWalletBalanceDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CourierWalletBalanceDto>>> GetWalletBalance(Guid id)
    {
        var query = new GetCourierWalletBalanceQuery { CourierAccountId = id };
        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return BadRequestResponse<CourierWalletBalanceDto>(string.Join(", ", result.Errors));

        return OkResponse(result.Value, "Wallet balance retrieved successfully.");
    }
}

/// <summary>
/// Request model for creating courier account.
/// </summary>
public record CreateCourierAccountApiRequest
{
    public string Name { get; init; } = string.Empty;
    public Domain.Enums.CourierType CourierType { get; init; }
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string? AccessToken { get; init; }
    public string? AccountId { get; init; }
    public string? ChannelId { get; init; }
    public bool IsDefault { get; init; }
    public int? Priority { get; init; }
}

/// <summary>
/// Request model for updating courier account.
/// </summary>
public record UpdateCourierAccountRequest
{
    public string? Name { get; init; }
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public string? AccountId { get; init; }
    public string? ChannelId { get; init; }
    public string? PickupLocation { get; init; }
    public bool? IsActive { get; init; }
}
