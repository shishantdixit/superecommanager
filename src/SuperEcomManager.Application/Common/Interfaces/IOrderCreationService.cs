using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for creating orders on external platforms.
/// Implementations handle platform-specific order creation logic.
/// </summary>
public interface IOrderCreationService
{
    /// <summary>
    /// Gets the channel type this service handles.
    /// </summary>
    ChannelType ChannelType { get; }

    /// <summary>
    /// Creates an order on the external platform.
    /// </summary>
    /// <param name="channelId">The channel ID to create the order for.</param>
    /// <param name="order">The internal order to push to the platform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the external order ID if successful.</returns>
    Task<OrderCreationResult> CreateOrderAsync(
        Guid channelId,
        Order order,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an external order creation operation.
/// </summary>
public class OrderCreationResult
{
    public bool Success { get; set; }
    public string? ExternalOrderId { get; set; }
    public string? ExternalOrderNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? PlatformData { get; set; }

    public static OrderCreationResult Succeeded(string externalOrderId, string? externalOrderNumber = null, Dictionary<string, object>? platformData = null)
        => new()
        {
            Success = true,
            ExternalOrderId = externalOrderId,
            ExternalOrderNumber = externalOrderNumber,
            PlatformData = platformData
        };

    public static OrderCreationResult Failed(string errorMessage)
        => new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Factory for creating order creation services based on channel type.
/// </summary>
public interface IOrderCreationServiceFactory
{
    /// <summary>
    /// Gets the order creation service for the specified channel type.
    /// Returns null if no service is registered for the channel type.
    /// </summary>
    /// <param name="channelType">The channel type.</param>
    /// <returns>The order creation service, or null if not supported.</returns>
    IOrderCreationService? GetService(ChannelType channelType);

    /// <summary>
    /// Checks if external order creation is supported for the given channel type.
    /// </summary>
    /// <param name="channelType">The channel type.</param>
    /// <returns>True if order creation is supported.</returns>
    bool IsSupported(ChannelType channelType);
}
