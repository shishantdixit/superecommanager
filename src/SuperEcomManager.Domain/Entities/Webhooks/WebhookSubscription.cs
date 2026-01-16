using SuperEcomManager.Domain.Common;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Domain.Entities.Webhooks;

/// <summary>
/// Represents a webhook subscription for receiving event notifications.
/// </summary>
public class WebhookSubscription : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string Secret { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public List<WebhookEvent> Events { get; private set; } = new();
    public Dictionary<string, string> Headers { get; private set; } = new();
    public int MaxRetries { get; private set; } = 3;
    public int TimeoutSeconds { get; private set; } = 30;
    public DateTime? LastTriggeredAt { get; private set; }
    public int TotalDeliveries { get; private set; }
    public int SuccessfulDeliveries { get; private set; }
    public int FailedDeliveries { get; private set; }

    private readonly List<WebhookDelivery> _deliveries = new();
    public IReadOnlyList<WebhookDelivery> Deliveries => _deliveries.AsReadOnly();

    private WebhookSubscription() { }

    public static WebhookSubscription Create(
        string name,
        string url,
        string secret,
        List<WebhookEvent> events,
        Dictionary<string, string>? headers = null,
        int maxRetries = 3,
        int timeoutSeconds = 30)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Webhook name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Webhook URL is required", nameof(url));

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("Invalid webhook URL", nameof(url));

        if (events == null || events.Count == 0)
            throw new ArgumentException("At least one event must be subscribed", nameof(events));

        return new WebhookSubscription
        {
            Id = Guid.NewGuid(),
            Name = name,
            Url = url,
            Secret = secret,
            Events = events,
            Headers = headers ?? new Dictionary<string, string>(),
            MaxRetries = maxRetries,
            TimeoutSeconds = timeoutSeconds,
            IsActive = true
        };
    }

    public void Update(
        string name,
        string url,
        List<WebhookEvent> events,
        Dictionary<string, string>? headers = null,
        int? maxRetries = null,
        int? timeoutSeconds = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;

        if (!string.IsNullOrWhiteSpace(url))
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
                throw new ArgumentException("Invalid webhook URL", nameof(url));

            Url = url;
        }

        if (events != null && events.Count > 0)
            Events = events;

        if (headers != null)
            Headers = headers;

        if (maxRetries.HasValue)
            MaxRetries = maxRetries.Value;

        if (timeoutSeconds.HasValue)
            TimeoutSeconds = timeoutSeconds.Value;
    }

    public void UpdateSecret(string newSecret)
    {
        if (string.IsNullOrWhiteSpace(newSecret))
            throw new ArgumentException("Secret cannot be empty", nameof(newSecret));

        Secret = newSecret;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void RecordDelivery(bool success)
    {
        TotalDeliveries++;
        LastTriggeredAt = DateTime.UtcNow;

        if (success)
            SuccessfulDeliveries++;
        else
            FailedDeliveries++;
    }

    public bool IsSubscribedTo(WebhookEvent eventType) => Events.Contains(eventType);
}
