using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Application.Common.Interfaces;
using SuperEcomManager.Application.Features.Webhooks;
using SuperEcomManager.Domain.Entities.Webhooks;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Infrastructure.Services;

/// <summary>
/// Service for dispatching webhook events to subscribed endpoints.
/// </summary>
public class WebhookDispatcherService : IWebhookDispatcher
{
    private readonly ITenantDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDispatcherService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public WebhookDispatcherService(
        ITenantDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDispatcherService> logger)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DispatchAsync<T>(
        WebhookEvent eventType,
        T data,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(w => w.IsActive && w.Events.Contains(eventType))
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug("No active webhook subscriptions for event {EventType}", eventType);
            return;
        }

        var payload = new WebhookPayloadDto
        {
            Id = Guid.NewGuid().ToString(),
            Event = eventType,
            Timestamp = DateTime.UtcNow,
            Data = data!
        };

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        foreach (var subscription in subscriptions)
        {
            try
            {
                await SendWebhookAsync(subscription, eventType, payloadJson, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch webhook {WebhookName} for event {EventType}",
                    subscription.Name, eventType);
            }
        }
    }

    public async Task<WebhookTestResult> TestAsync(
        string url,
        string secret,
        CancellationToken cancellationToken = default)
    {
        var testPayload = new WebhookPayloadDto
        {
            Id = Guid.NewGuid().ToString(),
            Event = WebhookEvent.OrderCreated,
            Timestamp = DateTime.UtcNow,
            Data = new { Test = true, Message = "Webhook test payload" }
        };

        var payloadJson = JsonSerializer.Serialize(testPayload, JsonOptions);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = _httpClientFactory.CreateClient("Webhook");
            client.Timeout = TimeSpan.FromSeconds(30);

            var request = CreateRequest(url, payloadJson, secret, null);
            var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var isSuccess = (int)response.StatusCode >= 200 && (int)response.StatusCode < 300;

            return new WebhookTestResult
            {
                Success = isSuccess,
                StatusCode = (int)response.StatusCode,
                ResponseBody = responseBody.Length > 1000 ? responseBody[..1000] + "..." : responseBody,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Webhook test failed for URL {Url}", url);

            return new WebhookTestResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    public async Task RetryFailedDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        var pendingDeliveries = await _dbContext.WebhookDeliveries
            .Include(d => d.Subscription)
            .Where(d => d.Status == WebhookDeliveryStatus.Retrying &&
                       d.NextRetryAt.HasValue &&
                       d.NextRetryAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var delivery in pendingDeliveries)
        {
            if (delivery.Subscription == null || !delivery.Subscription.IsActive)
            {
                delivery.MarkFailed("Subscription is inactive", null, null, TimeSpan.Zero, null);
                continue;
            }

            await RetryDeliveryAsync(delivery, delivery.Subscription, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SendWebhookAsync(
        WebhookSubscription subscription,
        WebhookEvent eventType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        // Create delivery record
        var delivery = WebhookDelivery.Create(subscription.Id, eventType, payloadJson);
        _dbContext.WebhookDeliveries.Add(delivery);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = _httpClientFactory.CreateClient("Webhook");
            client.Timeout = TimeSpan.FromSeconds(subscription.TimeoutSeconds);

            var request = CreateRequest(subscription.Url, payloadJson, subscription.Secret, subscription.Headers);
            var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var isSuccess = (int)response.StatusCode >= 200 && (int)response.StatusCode < 300;

            if (isSuccess)
            {
                delivery.MarkDelivered(
                    (int)response.StatusCode,
                    responseBody.Length > 1000 ? responseBody[..1000] + "..." : responseBody,
                    stopwatch.Elapsed);

                subscription.RecordDelivery(true);

                _logger.LogInformation(
                    "Webhook {WebhookName} delivered successfully for event {EventType}",
                    subscription.Name, eventType);
            }
            else
            {
                HandleDeliveryFailure(
                    delivery,
                    subscription,
                    $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                    (int)response.StatusCode,
                    responseBody,
                    stopwatch.Elapsed);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            HandleDeliveryFailure(delivery, subscription, ex.Message, null, null, stopwatch.Elapsed);

            _logger.LogError(ex, "Failed to deliver webhook {WebhookName} for event {EventType}",
                subscription.Name, eventType);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RetryDeliveryAsync(
        WebhookDelivery delivery,
        WebhookSubscription subscription,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = _httpClientFactory.CreateClient("Webhook");
            client.Timeout = TimeSpan.FromSeconds(subscription.TimeoutSeconds);

            var request = CreateRequest(subscription.Url, delivery.Payload, subscription.Secret, subscription.Headers);
            var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var isSuccess = (int)response.StatusCode >= 200 && (int)response.StatusCode < 300;

            if (isSuccess)
            {
                delivery.MarkDelivered(
                    (int)response.StatusCode,
                    responseBody.Length > 1000 ? responseBody[..1000] + "..." : responseBody,
                    stopwatch.Elapsed);

                subscription.RecordDelivery(true);

                _logger.LogInformation(
                    "Webhook {WebhookName} retry delivered successfully for event {EventType}",
                    subscription.Name, delivery.Event);
            }
            else
            {
                HandleDeliveryFailure(
                    delivery,
                    subscription,
                    $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                    (int)response.StatusCode,
                    responseBody,
                    stopwatch.Elapsed);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            HandleDeliveryFailure(delivery, subscription, ex.Message, null, null, stopwatch.Elapsed);

            _logger.LogError(ex, "Failed to retry webhook {WebhookName} for event {EventType}",
                subscription.Name, delivery.Event);
        }
    }

    private static void HandleDeliveryFailure(
        WebhookDelivery delivery,
        WebhookSubscription subscription,
        string errorMessage,
        int? statusCode,
        string? responseBody,
        TimeSpan duration)
    {
        DateTime? nextRetry = null;

        if (delivery.AttemptCount < subscription.MaxRetries)
        {
            // Exponential backoff: 1min, 5min, 30min
            var delayMinutes = Math.Pow(5, delivery.AttemptCount);
            nextRetry = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
        else
        {
            subscription.RecordDelivery(false);
        }

        delivery.MarkFailed(
            errorMessage,
            statusCode,
            responseBody?.Length > 1000 ? responseBody[..1000] + "..." : responseBody,
            duration,
            nextRetry);
    }

    private static HttpRequestMessage CreateRequest(
        string url,
        string payload,
        string secret,
        Dictionary<string, string>? customHeaders)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Add signature
        var signature = ComputeSignature(payload, secret);
        request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
        request.Headers.Add("X-Webhook-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        // Add custom headers
        if (customHeaders != null)
        {
            foreach (var header in customHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
