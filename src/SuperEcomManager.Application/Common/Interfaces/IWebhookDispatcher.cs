using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Interface for dispatching webhook events.
/// </summary>
public interface IWebhookDispatcher
{
    /// <summary>
    /// Dispatches a webhook event to all subscribed endpoints.
    /// </summary>
    /// <typeparam name="T">The type of the event data.</typeparam>
    /// <param name="eventType">The type of event.</param>
    /// <param name="data">The event payload data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DispatchAsync<T>(WebhookEvent eventType, T data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a webhook endpoint with a sample payload.
    /// </summary>
    /// <param name="url">The webhook URL to test.</param>
    /// <param name="secret">The webhook secret for signing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test result with status and response.</returns>
    Task<WebhookTestResult> TestAsync(string url, string secret, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries failed webhook deliveries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RetryFailedDeliveriesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a webhook test.
/// </summary>
public record WebhookTestResult
{
    public bool Success { get; init; }
    public int? StatusCode { get; init; }
    public string? ResponseBody { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
}
