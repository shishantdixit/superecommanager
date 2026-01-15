using SuperEcomManager.Integrations.Shopify.Models;

namespace SuperEcomManager.Integrations.Shopify.Webhooks;

/// <summary>
/// Interface for handling Shopify webhooks.
/// </summary>
public interface IShopifyWebhookHandler
{
    /// <summary>
    /// Handles an incoming webhook from Shopify.
    /// </summary>
    Task HandleWebhookAsync(
        string topic,
        string shopDomain,
        string payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Payload for order-related webhooks.
/// </summary>
public class ShopifyOrderWebhookPayload
{
    public ShopifyOrder? Order { get; set; }
}

/// <summary>
/// Supported Shopify webhook topics.
/// </summary>
public static class ShopifyWebhookTopics
{
    public const string OrdersCreate = "orders/create";
    public const string OrdersUpdated = "orders/updated";
    public const string OrdersFulfilled = "orders/fulfilled";
    public const string OrdersCancelled = "orders/cancelled";
    public const string OrdersPaid = "orders/paid";
    public const string RefundsCreate = "refunds/create";
    public const string AppUninstalled = "app/uninstalled";
}
