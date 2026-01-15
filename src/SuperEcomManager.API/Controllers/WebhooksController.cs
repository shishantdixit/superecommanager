using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Integrations.Couriers.Shiprocket;
using SuperEcomManager.Integrations.Couriers.Delhivery;
using SuperEcomManager.Integrations.Couriers.BlueDart;
using SuperEcomManager.Integrations.Couriers.DTDC;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Webhook endpoints for receiving callbacks from external services.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly ILogger<WebhooksController> _logger;
    private readonly IShiprocketWebhookHandler _shiprocketHandler;
    private readonly IDelhiveryWebhookHandler _delhiveryHandler;
    private readonly IBlueDartWebhookHandler _blueDartHandler;
    private readonly IDTDCWebhookHandler _dtdcHandler;

    // These will be injected from services configured per-tenant
    public Func<string, string, string, CancellationToken, Task>? HandleShopifyWebhook { get; set; }
    public Func<string, string, bool>? VerifyShopifySignature { get; set; }

    public WebhooksController(
        ILogger<WebhooksController> logger,
        IShiprocketWebhookHandler shiprocketHandler,
        IDelhiveryWebhookHandler delhiveryHandler,
        IBlueDartWebhookHandler blueDartHandler,
        IDTDCWebhookHandler dtdcHandler)
    {
        _logger = logger;
        _shiprocketHandler = shiprocketHandler;
        _delhiveryHandler = delhiveryHandler;
        _blueDartHandler = blueDartHandler;
        _dtdcHandler = dtdcHandler;
    }

    /// <summary>
    /// Receives webhooks from Shopify.
    /// </summary>
    [HttpPost("shopify")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ShopifyWebhook(CancellationToken cancellationToken)
    {
        // Read the raw request body
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        // Get webhook headers
        var hmacHeader = Request.Headers["X-Shopify-Hmac-SHA256"].FirstOrDefault();
        var shopDomain = Request.Headers["X-Shopify-Shop-Domain"].FirstOrDefault();
        var topic = Request.Headers["X-Shopify-Topic"].FirstOrDefault();

        if (string.IsNullOrEmpty(hmacHeader) || string.IsNullOrEmpty(shopDomain) || string.IsNullOrEmpty(topic))
        {
            _logger.LogWarning("Missing required Shopify webhook headers");
            return BadRequest("Missing required headers");
        }

        // Verify webhook signature
        if (VerifyShopifySignature != null && !VerifyShopifySignature(payload, hmacHeader))
        {
            _logger.LogWarning("Invalid Shopify webhook signature from {ShopDomain}", shopDomain);
            return Unauthorized("Invalid signature");
        }

        _logger.LogInformation("Received Shopify webhook: {Topic} from {ShopDomain}", topic, shopDomain);

        try
        {
            // Process webhook asynchronously
            if (HandleShopifyWebhook != null)
            {
                // In production, you would typically queue this for background processing
                await HandleShopifyWebhook(topic, shopDomain, payload, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Shopify webhook handler not configured");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Shopify webhook {Topic}", topic);
            // Return 200 to acknowledge receipt - Shopify will retry on non-2xx responses
            // The error is logged and can be handled through monitoring
            return Ok();
        }
    }

    /// <summary>
    /// Health check endpoint for webhooks.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult WebhookHealth()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    #region Courier Webhooks

    /// <summary>
    /// Receives webhooks from Shiprocket for shipment status updates.
    /// </summary>
    [HttpPost("shiprocket")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ShiprocketWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrEmpty(payload))
        {
            _logger.LogWarning("Empty Shiprocket webhook payload received");
            return BadRequest("Empty payload");
        }

        _logger.LogInformation("Received Shiprocket webhook");

        try
        {
            var result = await _shiprocketHandler.HandleWebhookAsync(payload, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("Shiprocket webhook processing failed: {Message}", result.Message);
            }
            else
            {
                _logger.LogInformation(
                    "Shiprocket webhook processed: AWB={Awb}, Status={Status}",
                    result.AwbNumber,
                    result.NewStatus);

                // TODO: Update shipment status in database
                // This would typically be done via a MediatR command or domain event
            }

            // Return 200 to acknowledge receipt
            return Ok(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Shiprocket webhook");
            return Ok(new { success = false, message = "Error processing webhook" });
        }
    }

    /// <summary>
    /// Receives webhooks from Delhivery for shipment status updates.
    /// </summary>
    [HttpPost("delhivery")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DelhiveryWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrEmpty(payload))
        {
            _logger.LogWarning("Empty Delhivery webhook payload received");
            return BadRequest("Empty payload");
        }

        _logger.LogInformation("Received Delhivery webhook");

        try
        {
            var result = await _delhiveryHandler.HandleWebhookAsync(payload, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("Delhivery webhook processing failed: {Message}", result.Message);
            }
            else
            {
                _logger.LogInformation(
                    "Delhivery webhook processed: AWB={Awb}, Status={Status}",
                    result.AwbNumber,
                    result.NewStatus);
            }

            return Ok(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Delhivery webhook");
            return Ok(new { success = false, message = "Error processing webhook" });
        }
    }

    /// <summary>
    /// Receives webhooks from BlueDart for shipment status updates.
    /// </summary>
    [HttpPost("bluedart")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BlueDartWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrEmpty(payload))
        {
            _logger.LogWarning("Empty BlueDart webhook payload received");
            return BadRequest("Empty payload");
        }

        _logger.LogInformation("Received BlueDart webhook");

        try
        {
            var result = await _blueDartHandler.HandleWebhookAsync(payload, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("BlueDart webhook processing failed: {Message}", result.Message);
            }
            else
            {
                _logger.LogInformation(
                    "BlueDart webhook processed: AWB={Awb}, Status={Status}",
                    result.AwbNumber,
                    result.NewStatus);
            }

            return Ok(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing BlueDart webhook");
            return Ok(new { success = false, message = "Error processing webhook" });
        }
    }

    /// <summary>
    /// Receives webhooks from DTDC for shipment status updates.
    /// </summary>
    [HttpPost("dtdc")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DTDCWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrEmpty(payload))
        {
            _logger.LogWarning("Empty DTDC webhook payload received");
            return BadRequest("Empty payload");
        }

        _logger.LogInformation("Received DTDC webhook");

        try
        {
            var result = await _dtdcHandler.HandleWebhookAsync(payload, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("DTDC webhook processing failed: {Message}", result.Message);
            }
            else
            {
                _logger.LogInformation(
                    "DTDC webhook processed: AWB={Awb}, Status={Status}",
                    result.AwbNumber,
                    result.NewStatus);
            }

            return Ok(new { success = result.Success, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DTDC webhook");
            return Ok(new { success = false, message = "Error processing webhook" });
        }
    }

    #endregion
}
