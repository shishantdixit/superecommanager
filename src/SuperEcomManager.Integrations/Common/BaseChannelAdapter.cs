using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Integrations.Common;

/// <summary>
/// Base abstract class for channel adapters with common functionality.
/// </summary>
public abstract class BaseChannelAdapter : IChannelAdapter
{
    protected readonly ILogger Logger;
    protected readonly IHttpClientFactory HttpClientFactory;

    protected BaseChannelAdapter(
        ILogger logger,
        IHttpClientFactory httpClientFactory)
    {
        Logger = logger;
        HttpClientFactory = httpClientFactory;
    }

    public abstract ChannelType ChannelType { get; }
    public abstract string DisplayName { get; }
    public virtual bool SupportsOAuth => false;
    public virtual bool SupportsInventorySync => true;
    public virtual bool SupportsOrderCancellation => false;

    public abstract Task<ChannelConnectionResult> ValidateConnectionAsync(
        ChannelCredentials credentials,
        CancellationToken cancellationToken = default);

    public abstract Task<ChannelSyncResult> SyncOrdersAsync(
        Guid channelId,
        ChannelCredentials credentials,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    public virtual Task<ChannelSyncResult> SyncInventoryAsync(
        Guid channelId,
        ChannelCredentials credentials,
        IEnumerable<InventorySyncItem> items,
        CancellationToken cancellationToken = default)
    {
        Logger.LogWarning("Inventory sync not implemented for {ChannelType}", ChannelType);
        return Task.FromResult(ChannelSyncResult.Failed("Inventory sync not supported"));
    }

    public virtual Task<ChannelOperationResult> UpdateShipmentAsync(
        ChannelCredentials credentials,
        ShipmentUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        Logger.LogWarning("Shipment update not implemented for {ChannelType}", ChannelType);
        return Task.FromResult(ChannelOperationResult.Failed("Shipment update not supported"));
    }

    public virtual Task<ChannelOperationResult> CancelOrderAsync(
        ChannelCredentials credentials,
        string externalOrderId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        Logger.LogWarning("Order cancellation not implemented for {ChannelType}", ChannelType);
        return Task.FromResult(ChannelOperationResult.Failed("Order cancellation not supported"));
    }

    public virtual Task<ChannelOrder?> GetOrderAsync(
        ChannelCredentials credentials,
        string externalOrderId,
        CancellationToken cancellationToken = default)
    {
        Logger.LogWarning("Get order not implemented for {ChannelType}", ChannelType);
        return Task.FromResult<ChannelOrder?>(null);
    }

    public virtual Task<string?> GetOAuthUrlAsync(
        string redirectUri,
        string state,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }

    public virtual Task<ChannelCredentials?> CompleteOAuthAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ChannelCredentials?>(null);
    }

    /// <summary>
    /// Creates an HTTP client for API calls.
    /// </summary>
    protected HttpClient CreateHttpClient(string? name = null)
    {
        return HttpClientFactory.CreateClient(name ?? ChannelType.ToString());
    }

    /// <summary>
    /// Logs an API call for debugging.
    /// </summary>
    protected void LogApiCall(string method, string endpoint, object? request = null)
    {
        Logger.LogDebug(
            "[{ChannelType}] API {Method} {Endpoint}",
            ChannelType, method, endpoint);
    }

    /// <summary>
    /// Logs an API response for debugging.
    /// </summary>
    protected void LogApiResponse(string endpoint, int statusCode, bool success)
    {
        if (success)
        {
            Logger.LogDebug(
                "[{ChannelType}] API response from {Endpoint}: {StatusCode}",
                ChannelType, endpoint, statusCode);
        }
        else
        {
            Logger.LogWarning(
                "[{ChannelType}] API error from {Endpoint}: {StatusCode}",
                ChannelType, endpoint, statusCode);
        }
    }

    /// <summary>
    /// Handles API errors with consistent logging.
    /// </summary>
    protected void LogApiError(string operation, Exception ex)
    {
        Logger.LogError(ex,
            "[{ChannelType}] Error during {Operation}: {Message}",
            ChannelType, operation, ex.Message);
    }
}
