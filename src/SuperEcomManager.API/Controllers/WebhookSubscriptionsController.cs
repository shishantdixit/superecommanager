using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Features.Webhooks;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.API.Controllers;

/// <summary>
/// Controller for managing outbound webhook subscriptions and viewing delivery logs.
/// </summary>
[Authorize]
[ApiController]
[Route("api/webhook-subscriptions")]
public class WebhookSubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebhookDispatcher _webhookDispatcher;

    public WebhookSubscriptionsController(IMediator mediator, IWebhookDispatcher webhookDispatcher)
    {
        _mediator = mediator;
        _webhookDispatcher = webhookDispatcher;
    }

    /// <summary>
    /// Gets all webhook subscriptions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WebhookSubscriptionListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions([FromQuery] bool? isActive = null)
    {
        var query = new GetWebhookSubscriptionsQuery { IsActive = isActive };
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors.FirstOrDefault());
    }

    /// <summary>
    /// Gets a specific webhook subscription by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WebhookSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription(Guid id)
    {
        var query = new GetWebhookSubscriptionQuery { Id = id };
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors.FirstOrDefault());
    }

    /// <summary>
    /// Creates a new webhook subscription.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WebhookSubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateWebhookSubscriptionRequestDto request)
    {
        var command = new CreateWebhookSubscriptionCommand
        {
            Name = request.Name,
            Url = request.Url,
            Events = request.Events,
            Headers = request.Headers,
            MaxRetries = request.MaxRetries,
            TimeoutSeconds = request.TimeoutSeconds
        };

        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetSubscription), new { id = result.Value!.Id }, result.Value)
            : BadRequest(result.Errors.FirstOrDefault());
    }

    /// <summary>
    /// Updates an existing webhook subscription.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WebhookSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] UpdateWebhookSubscriptionRequestDto request)
    {
        var command = new UpdateWebhookSubscriptionCommand
        {
            Id = id,
            Name = request.Name,
            Url = request.Url,
            Events = request.Events,
            Headers = request.Headers,
            MaxRetries = request.MaxRetries,
            TimeoutSeconds = request.TimeoutSeconds
        };

        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors.FirstOrDefault());
    }

    /// <summary>
    /// Toggles webhook subscription active status.
    /// </summary>
    [HttpPatch("{id:guid}/toggle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ToggleSubscription(Guid id, [FromBody] ToggleWebhookRequest request)
    {
        var command = new ToggleWebhookSubscriptionCommand
        {
            Id = id,
            IsActive = request.IsActive
        };

        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(new { IsActive = result.Value }) : BadRequest(result.Errors.FirstOrDefault());
    }

    /// <summary>
    /// Deletes a webhook subscription.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteSubscription(Guid id)
    {
        var command = new DeleteWebhookSubscriptionCommand { Id = id };
        var result = await _mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Errors.FirstOrDefault());
    }

    /// <summary>
    /// Regenerates the secret for a webhook subscription.
    /// </summary>
    [HttpPost("{id:guid}/regenerate-secret")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegenerateSecret(Guid id)
    {
        var command = new RegenerateWebhookSecretCommand { Id = id };
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(new { Secret = result.Value }) : BadRequest(result.Errors.FirstOrDefault());
    }

    /// <summary>
    /// Tests a webhook URL before creating subscription.
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(WebhookTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestWebhookUrl([FromBody] TestWebhookUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("URL is required");

        var testResult = await _webhookDispatcher.TestAsync(
            request.Url,
            request.Secret ?? "test-secret",
            HttpContext.RequestAborted);

        return Ok(new WebhookTestResultDto
        {
            Success = testResult.Success,
            StatusCode = testResult.StatusCode,
            ResponseBody = testResult.ResponseBody,
            ErrorMessage = testResult.ErrorMessage,
            Duration = testResult.Duration
        });
    }

    /// <summary>
    /// Gets webhook deliveries with optional filters.
    /// </summary>
    [HttpGet("deliveries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveries(
        [FromQuery] Guid? subscriptionId = null,
        [FromQuery] WebhookDeliveryStatus? status = null,
        [FromQuery] WebhookEvent? webhookEvent = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetWebhookDeliveriesQuery
        {
            SubscriptionId = subscriptionId,
            Status = status,
            Event = webhookEvent,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets webhook delivery details including payload.
    /// </summary>
    [HttpGet("deliveries/{id:guid}")]
    [ProducesResponseType(typeof(WebhookDeliveryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeliveryDetail(Guid id)
    {
        var query = new GetWebhookDeliveryDetailQuery { Id = id };
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Errors.FirstOrDefault());
    }

    /// <summary>
    /// Gets webhook statistics.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(WebhookStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats([FromQuery] int days = 30)
    {
        var query = new GetWebhookStatsQuery { Days = days };
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors.FirstOrDefault());
    }

    /// <summary>
    /// Gets list of available webhook events.
    /// </summary>
    [HttpGet("events")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents()
    {
        var query = new GetWebhookEventsQuery();
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors.FirstOrDefault());
    }
}

public record ToggleWebhookRequest
{
    public bool IsActive { get; init; }
}

public record TestWebhookUrlRequest
{
    public string Url { get; init; } = string.Empty;
    public string? Secret { get; init; }
}
