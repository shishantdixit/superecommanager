using SuperEcomManager.Integrations.Couriers.Shiprocket.Models;

namespace SuperEcomManager.Integrations.Couriers.Shiprocket;

/// <summary>
/// HTTP client interface for Shiprocket API.
/// </summary>
public interface IShiprocketClient
{
    /// <summary>
    /// Authenticates with Shiprocket and gets access token.
    /// </summary>
    Task<ShiprocketAuthResponse?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an adhoc order in Shiprocket (POST /orders/create/adhoc).
    /// </summary>
    Task<ShiprocketCreateOrderResponse?> CreateOrderAsync(
        string token,
        ShiprocketCreateOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a channel-specific order in Shiprocket (POST /orders/create).
    /// Requires channel_id in the request.
    /// </summary>
    Task<ShiprocketCreateOrderResponse?> CreateChannelOrderAsync(
        string token,
        ShiprocketCreateOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates AWB for a shipment.
    /// </summary>
    Task<ShiprocketAwbResponse?> GenerateAwbAsync(
        string token,
        ShiprocketGenerateAwbRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets courier serviceability and rates.
    /// </summary>
    Task<ShiprocketServiceabilityResponse?> CheckServiceabilityAsync(
        string token,
        string pickupPincode,
        string deliveryPincode,
        decimal weight,
        bool isCod,
        long? orderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tracking information for an AWB.
    /// </summary>
    Task<ShiprocketTrackingResponse?> GetTrackingAsync(
        string token,
        string awbCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules pickup for shipments.
    /// </summary>
    Task<ShiprocketPickupResponse?> SchedulePickupAsync(
        string token,
        List<long> shipmentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a shipment.
    /// </summary>
    Task<ShiprocketCancelResponse?> CancelShipmentAsync(
        string token,
        List<long> orderIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shipping label URL.
    /// </summary>
    Task<ShiprocketLabelResponse?> GetLabelAsync(
        string token,
        List<long> shipmentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads label PDF as bytes.
    /// </summary>
    Task<byte[]?> DownloadLabelAsync(
        string labelUrl,
        CancellationToken cancellationToken = default);

    // ========== ORDERS ==========

    /// <summary>
    /// Gets all orders with pagination and filters.
    /// </summary>
    Task<ShiprocketOrdersResponse?> GetOrdersAsync(
        string token,
        int page = 1,
        int perPage = 10,
        string? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific order by ID.
    /// </summary>
    Task<ShiprocketOrderDetailResponse?> GetOrderByIdAsync(
        string token,
        long orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    Task<ShiprocketUpdateOrderResponse?> UpdateOrderAsync(
        string token,
        long orderId,
        ShiprocketUpdateOrderRequest request,
        CancellationToken cancellationToken = default);

    // ========== SHIPMENTS ==========

    /// <summary>
    /// Gets all shipments with pagination.
    /// </summary>
    Task<ShiprocketShipmentsResponse?> GetShipmentsAsync(
        string token,
        int page = 1,
        int perPage = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific shipment by ID.
    /// </summary>
    Task<ShiprocketShipmentDetailResponse?> GetShipmentByIdAsync(
        string token,
        long shipmentId,
        CancellationToken cancellationToken = default);

    // ========== RETURNS ==========

    /// <summary>
    /// Creates a return order.
    /// </summary>
    Task<ShiprocketReturnResponse?> CreateReturnAsync(
        string token,
        ShiprocketCreateReturnRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all return orders.
    /// </summary>
    Task<ShiprocketReturnsListResponse?> GetReturnsAsync(
        string token,
        int page = 1,
        int perPage = 10,
        CancellationToken cancellationToken = default);

    // ========== MANIFEST ==========

    /// <summary>
    /// Generates manifest for shipments.
    /// </summary>
    Task<ShiprocketManifestResponse?> GenerateManifestAsync(
        string token,
        List<long> shipmentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prints manifest.
    /// </summary>
    Task<ShiprocketPrintManifestResponse?> PrintManifestAsync(
        string token,
        List<long> orderIds,
        CancellationToken cancellationToken = default);

    // ========== PICKUP ==========

    /// <summary>
    /// Cancels a scheduled pickup.
    /// </summary>
    Task<ShiprocketCancelPickupResponse?> CancelPickupAsync(
        string token,
        List<long> shipmentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pickup locations configured in the Shiprocket account.
    /// </summary>
    Task<ShiprocketPickupLocationsResponse?> GetPickupLocationsAsync(
        string token,
        CancellationToken cancellationToken = default);

    // ========== WALLET ==========

    /// <summary>
    /// Gets wallet balance.
    /// </summary>
    Task<ShiprocketWalletResponse?> GetWalletBalanceAsync(
        string token,
        CancellationToken cancellationToken = default);

    // ========== CHANNELS ==========

    /// <summary>
    /// Gets all channels.
    /// </summary>
    Task<ShiprocketChannelsResponse?> GetChannelsAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new channel.
    /// </summary>
    Task<ShiprocketChannelResponse?> CreateChannelAsync(
        string token,
        ShiprocketCreateChannelRequest request,
        CancellationToken cancellationToken = default);

    // ========== INVENTORY ==========

    /// <summary>
    /// Gets products from inventory.
    /// </summary>
    Task<ShiprocketProductsResponse?> GetProductsAsync(
        string token,
        int page = 1,
        int perPage = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds products to inventory.
    /// </summary>
    Task<ShiprocketAddProductResponse?> AddProductAsync(
        string token,
        ShiprocketAddProductRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates product inventory.
    /// </summary>
    Task<ShiprocketUpdateInventoryResponse?> UpdateInventoryAsync(
        string token,
        ShiprocketUpdateInventoryRequest request,
        CancellationToken cancellationToken = default);

    // ========== COURIER PARTNERS ==========

    /// <summary>
    /// Gets all available courier partners.
    /// </summary>
    Task<ShiprocketCourierPartnersResponse?> GetCourierPartnersAsync(
        string token,
        CancellationToken cancellationToken = default);

    // ========== NDR ==========

    /// <summary>
    /// Updates NDR action for a shipment.
    /// </summary>
    Task<ShiprocketNdrResponse?> UpdateNdrActionAsync(
        string token,
        ShiprocketNdrActionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets NDR details.
    /// </summary>
    Task<ShiprocketNdrListResponse?> GetNdrListAsync(
        string token,
        int page = 1,
        int perPage = 10,
        CancellationToken cancellationToken = default);

    // ========== WEIGHT RECONCILIATION ==========

    /// <summary>
    /// Gets weight disputes.
    /// </summary>
    Task<ShiprocketWeightDisputesResponse?> GetWeightDisputesAsync(
        string token,
        int page = 1,
        int perPage = 10,
        CancellationToken cancellationToken = default);

    // ========== WEBHOOKS ==========

    /// <summary>
    /// Creates a webhook.
    /// </summary>
    Task<ShiprocketWebhookResponse?> CreateWebhookAsync(
        string token,
        ShiprocketCreateWebhookRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a webhook.
    /// </summary>
    Task<ShiprocketWebhookResponse?> UpdateWebhookAsync(
        string token,
        long webhookId,
        ShiprocketUpdateWebhookRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a webhook.
    /// </summary>
    Task<ShiprocketDeleteWebhookResponse?> DeleteWebhookAsync(
        string token,
        long webhookId,
        CancellationToken cancellationToken = default);
}
