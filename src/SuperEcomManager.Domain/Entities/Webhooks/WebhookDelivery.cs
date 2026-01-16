using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Webhooks;

/// <summary>
/// Represents a webhook delivery attempt.
/// </summary>
public class WebhookDelivery : BaseEntity
{
    public Guid WebhookSubscriptionId { get; private set; }
    public WebhookEvent Event { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public WebhookDeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int? HttpStatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public TimeSpan? Duration { get; private set; }

    public WebhookSubscription? Subscription { get; private set; }

    private WebhookDelivery() { }

    public static WebhookDelivery Create(
        Guid subscriptionId,
        WebhookEvent eventType,
        string payload)
    {
        return new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookSubscriptionId = subscriptionId,
            Event = eventType,
            Payload = payload,
            Status = WebhookDeliveryStatus.Pending,
            AttemptCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkDelivered(int httpStatusCode, string? responseBody, TimeSpan duration)
    {
        AttemptCount++;
        HttpStatusCode = httpStatusCode;
        ResponseBody = responseBody;
        Status = WebhookDeliveryStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        Duration = duration;
        NextRetryAt = null;
    }

    public void MarkFailed(string errorMessage, int? httpStatusCode, string? responseBody, TimeSpan duration, DateTime? nextRetry)
    {
        AttemptCount++;
        ErrorMessage = errorMessage;
        HttpStatusCode = httpStatusCode;
        ResponseBody = responseBody;
        Duration = duration;

        if (nextRetry.HasValue)
        {
            Status = WebhookDeliveryStatus.Retrying;
            NextRetryAt = nextRetry;
        }
        else
        {
            Status = WebhookDeliveryStatus.Failed;
            NextRetryAt = null;
        }
    }
}
