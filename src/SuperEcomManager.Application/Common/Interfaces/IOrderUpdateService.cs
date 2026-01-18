using SuperEcomManager.Domain.Entities.Orders;
using SuperEcomManager.Domain.Enums;

namespace SuperEcomManager.Application.Common.Interfaces;

/// <summary>
/// Service for updating orders on external platforms.
/// Implementations handle platform-specific order update logic.
/// </summary>
public interface IOrderUpdateService
{
    /// <summary>
    /// Gets the channel type this service handles.
    /// </summary>
    ChannelType ChannelType { get; }

    /// <summary>
    /// Updates an order on the external platform.
    /// </summary>
    /// <param name="channelId">The channel ID the order belongs to.</param>
    /// <param name="externalOrderId">The external order ID on the platform.</param>
    /// <param name="order">The internal order with updated data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<OrderUpdateResult> UpdateOrderAsync(
        Guid channelId,
        string externalOrderId,
        Order order,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an external order update operation.
/// </summary>
public class OrderUpdateResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? PlatformData { get; set; }

    public static OrderUpdateResult Succeeded(Dictionary<string, object>? platformData = null)
        => new()
        {
            Success = true,
            PlatformData = platformData
        };

    public static OrderUpdateResult Failed(string errorMessage)
        => new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Factory for creating order update services based on channel type.
/// </summary>
public interface IOrderUpdateServiceFactory
{
    /// <summary>
    /// Gets the order update service for the specified channel type.
    /// Returns null if no service is registered for the channel type.
    /// </summary>
    /// <param name="channelType">The channel type.</param>
    /// <returns>The order update service, or null if not supported.</returns>
    IOrderUpdateService? GetService(ChannelType channelType);

    /// <summary>
    /// Checks if external order updates are supported for the given channel type.
    /// </summary>
    /// <param name="channelType">The channel type.</param>
    /// <returns>True if order updates are supported.</returns>
    bool IsSupported(ChannelType channelType);
}
