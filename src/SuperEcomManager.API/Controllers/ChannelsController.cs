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
    private readonly IConfiguration _configuration;

    public ChannelsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetFrontendUrl()
    {
        // Get first allowed origin as frontend URL
        var origins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        return origins?.FirstOrDefault() ?? "http://localhost:3000";
    }
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
    /// Save Shopify API credentials for a tenant.
    /// Each tenant must create their own Shopify app and provide their credentials here.
    /// </summary>
    [HttpPost("shopify/credentials")]
    [ProducesResponseType(typeof(ApiResponse<ChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ChannelDto>>> SaveShopifyCredentials(
        [FromBody] SaveShopifyCredentialsRequest request)
    {
        var result = await Mediator.Send(new SaveShopifyCredentialsCommand
        {
            ChannelId = request.ChannelId,
            ApiKey = request.ApiKey,
            ApiSecret = request.ApiSecret,
            ShopDomain = request.ShopDomain,
            Scopes = request.Scopes
        });

        if (!result.IsSuccess)
            return BadRequestResponse<ChannelDto>(result.Errors.FirstOrDefault() ?? "Failed to save credentials");

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Initiate Shopify OAuth connection for a channel that has credentials saved.
    /// </summary>
    [HttpPost("{id:guid}/shopify/connect")]
    [ProducesResponseType(typeof(ApiResponse<ShopifyOAuthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ShopifyOAuthResult>>> ConnectShopify(Guid id)
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/channels/shopify/callback";

        var result = await Mediator.Send(new InitiateShopifyOAuthCommand
        {
            ChannelId = id,
            RedirectUri = redirectUri
        });

        if (!result.IsSuccess)
            return BadRequestResponse<ShopifyOAuthResult>(result.Errors.FirstOrDefault() ?? "Failed to initiate OAuth");

        return OkResponse(result.Value!);
    }

    /// <summary>
    /// Shopify OAuth callback handler.
    /// This endpoint does not require authentication as it's called directly by Shopify.
    /// Tenant context is restored from the OAuth state stored in cache.
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
        var frontendUrl = GetFrontendUrl();

        var result = await Mediator.Send(new CompleteShopifyOAuthCommand
        {
            Code = code,
            State = state,
            ShopDomain = shop
        });

        if (!result.IsSuccess)
        {
            // Redirect to frontend with error
            var errorMessage = Uri.EscapeDataString(result.Errors.FirstOrDefault() ?? "Connection failed");
            return Redirect($"{frontendUrl}/channels/shopify?error={errorMessage}");
        }

        // Redirect to frontend channel settings page with success
        return Redirect($"{frontendUrl}/channels/{result.Value!.Id}?connected=true");
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

    /// <summary>
    /// Update channel settings.
    /// </summary>
    [HttpPost("{id:guid}/settings")]
    [ProducesResponseType(typeof(ApiResponse<ChannelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ChannelDto>>> UpdateChannelSettings(
        Guid id,
        [FromBody] UpdateChannelSettingsRequest request)
    {
        var result = await Mediator.Send(new UpdateChannelSettingsCommand
        {
            ChannelId = id,
            AutoSyncOrders = request.AutoSyncOrders,
            AutoSyncInventory = request.AutoSyncInventory,
            InitialSyncDays = request.InitialSyncDays,
            SyncProductsEnabled = request.SyncProductsEnabled,
            AutoSyncProducts = request.AutoSyncProducts
        });

        if (!result.IsSuccess)
            return BadRequestResponse<ChannelDto>(result.Errors.FirstOrDefault() ?? "Update failed");

        return OkResponse(result.Value!);
    }
}
