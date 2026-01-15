using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Models;
using SuperEcomManager.Application.Features.Channels;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Sales channel management endpoints.
/// </summary>
[Authorize]
public class ChannelsController : ApiControllerBase
{
    /// <summary>
    /// Get all sales channels.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChannelDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ChannelDto>>>> GetChannels()
    {
        var channels = await Mediator.Send(new GetChannelsQuery());
        return OkResponse(channels);
    }

    /// <summary>
    /// Get channel by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChannelDto>>> GetChannel(Guid id)
    {
        var channel = await Mediator.Send(new GetChannelByIdQuery { Id = id });
        if (channel == null)
            return NotFoundResponse<ChannelDto>("Channel not found");

        return OkResponse(channel);
    }

    /// <summary>
    /// Initiate Shopify OAuth connection.
    /// </summary>
    [HttpPost("shopify/connect")]
    [ProducesResponseType(typeof(ApiResponse<ShopifyOAuthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ShopifyOAuthResult>>> ConnectShopify(
        [FromBody] ConnectShopifyRequest request)
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/channels/shopify/callback";

        var result = await Mediator.Send(new InitiateShopifyOAuthCommand
        {
            ShopDomain = request.ShopDomain,
            RedirectUri = redirectUri
        });

        if (!result.IsSuccess)
            return BadRequestResponse<ShopifyOAuthResult>(result.Errors.FirstOrDefault() ?? "Failed to initiate OAuth");

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Shopify OAuth callback handler.
    /// </summary>
    [HttpGet("shopify/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ShopifyCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string shop)
    {
        var result = await Mediator.Send(new CompleteShopifyOAuthCommand
        {
            Code = code,
            State = state,
            ShopDomain = shop
        });

        if (!result.IsSuccess)
        {
            // Redirect to frontend with error
            return Redirect($"/settings/channels?error={Uri.EscapeDataString(result.Errors.FirstOrDefault() ?? "Connection failed")}");
        }

        // Redirect to frontend with success
        return Redirect($"/settings/channels?connected=true&channel={result.Value!.Id}");
    }

    /// <summary>
    /// Disconnect a sales channel.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisconnectChannel(Guid id)
    {
        var result = await Mediator.Send(new DisconnectChannelCommand { ChannelId = id });

        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Errors.FirstOrDefault() ?? "Channel not found"));

        return NoContent();
    }

    /// <summary>
    /// Trigger manual sync for a channel.
    /// </summary>
    [HttpPost("{id:guid}/sync")]
    [ProducesResponseType(typeof(ApiResponse<ChannelSyncResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChannelSyncResult>>> SyncChannel(Guid id)
    {
        var result = await Mediator.Send(new SyncChannelCommand { ChannelId = id });

        if (!result.IsSuccess)
            return BadRequestResponse<ChannelSyncResult>(result.Errors.FirstOrDefault() ?? "Sync failed");

        return OkResponse(result.Value!);
    }
}
