using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Features.Webhooks;

/// <summary>
/// DTO for webhook subscription.
/// </summary>
public record WebhookSubscriptionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public List<WebhookEvent> Events { get; init; } = new();
    public Dictionary<string, string> Headers { get; init; } = new();
    public int MaxRetries { get; init; }
    public int TimeoutSeconds { get; init; }
    public DateTime? LastTriggeredAt { get; init; }
    public int TotalDeliveries { get; init; }
    public int SuccessfulDeliveries { get; init; }
    public int FailedDeliveries { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// DTO for webhook subscription list item.
/// </summary>
public record WebhookSubscriptionListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int EventCount { get; init; }
    public DateTime? LastTriggeredAt { get; init; }
    public int TotalDeliveries { get; init; }
    public double SuccessRate { get; init; }
}

/// <summary>
/// DTO for webhook delivery.
/// </summary>
public record WebhookDeliveryDto
{
    public Guid Id { get; init; }
    public Guid WebhookSubscriptionId { get; init; }
    public string WebhookName { get; init; } = string.Empty;
    public WebhookEvent Event { get; init; }
    public WebhookDeliveryStatus Status { get; init; }
    public int AttemptCount { get; init; }
    public int? HttpStatusCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public TimeSpan? Duration { get; init; }
}

/// <summary>
/// DTO for webhook delivery details with payload.
/// </summary>
public record WebhookDeliveryDetailDto : WebhookDeliveryDto
{
    public string Payload { get; init; } = string.Empty;
    public string? ResponseBody { get; init; }
}

/// <summary>
/// Request to create a webhook subscription.
/// </summary>
public record CreateWebhookSubscriptionRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public List<WebhookEvent> Events { get; init; } = new();
    public Dictionary<string, string>? Headers { get; init; }
    public int MaxRetries { get; init; } = 3;
    public int TimeoutSeconds { get; init; } = 30;
}

/// <summary>
/// Request to update a webhook subscription.
/// </summary>
public record UpdateWebhookSubscriptionRequestDto
{
    public string? Name { get; init; }
    public string? Url { get; init; }
    public List<WebhookEvent>? Events { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
    public int? MaxRetries { get; init; }
    public int? TimeoutSeconds { get; init; }
}

/// <summary>
/// DTO for webhook test result.
/// </summary>
public record WebhookTestResultDto
{
    public bool Success { get; init; }
    public int? StatusCode { get; init; }
    public string? ResponseBody { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// DTO for webhook statistics.
/// </summary>
public record WebhookStatsDto
{
    public int TotalSubscriptions { get; init; }
    public int ActiveSubscriptions { get; init; }
    public int TotalDeliveries { get; init; }
    public int SuccessfulDeliveries { get; init; }
    public int FailedDeliveries { get; init; }
    public int PendingDeliveries { get; init; }
    public double OverallSuccessRate { get; init; }
    public List<WebhookEventStatsDto> EventStats { get; init; } = new();
}

/// <summary>
/// DTO for webhook event statistics.
/// </summary>
public record WebhookEventStatsDto
{
    public WebhookEvent Event { get; init; }
    public int TotalDeliveries { get; init; }
    public int SuccessfulDeliveries { get; init; }
    public int FailedDeliveries { get; init; }
    public double SuccessRate { get; init; }
}

/// <summary>
/// Webhook payload wrapper.
/// </summary>
public record WebhookPayloadDto
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public WebhookEvent Event { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public object Data { get; init; } = new { };
}
