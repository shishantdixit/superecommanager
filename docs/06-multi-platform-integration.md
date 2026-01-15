# Multi-Platform Integration

## Table of Contents
1. [Overview](#overview)
2. [Adapter Pattern Architecture](#adapter-pattern-architecture)
3. [Channel Adapters](#channel-adapters)
4. [Courier Adapters](#courier-adapters)
5. [Unified Data Models](#unified-data-models)
6. [Webhook Processing](#webhook-processing)
7. [Sync Strategies](#sync-strategies)
8. [Error Handling](#error-handling)

---

## Overview

SuperEcomManager uses the **Adapter Pattern** to integrate with multiple sales channels and courier services. This provides:

- **Consistent API** - Uniform interface regardless of platform
- **Easy extensibility** - Add new platforms without changing core logic
- **Isolated changes** - Platform API changes contained within adapter
- **Testability** - Mock adapters for testing

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                        INTEGRATION ARCHITECTURE                                      │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│                            ┌─────────────────────┐                                  │
│                            │   Application       │                                  │
│                            │   Services          │                                  │
│                            └──────────┬──────────┘                                  │
│                                       │                                              │
│                    ┌──────────────────┴──────────────────┐                          │
│                    │           Adapter Factory           │                          │
│                    └──────────────────┬──────────────────┘                          │
│                                       │                                              │
│        ┌──────────────┬───────────────┼───────────────┬──────────────┐              │
│        ▼              ▼               ▼               ▼              ▼              │
│   ┌─────────┐   ┌─────────┐   ┌───────────┐   ┌─────────┐   ┌────────────┐        │
│   │ Shopify │   │ Amazon  │   │ Flipkart  │   │ Meesho  │   │WooCommerce │        │
│   │ Adapter │   │ Adapter │   │  Adapter  │   │ Adapter │   │  Adapter   │        │
│   └────┬────┘   └────┬────┘   └─────┬─────┘   └────┬────┘   └─────┬──────┘        │
│        │             │              │              │              │                │
│        ▼             ▼              ▼              ▼              ▼                │
│   ┌─────────┐   ┌─────────┐   ┌───────────┐   ┌─────────┐   ┌────────────┐        │
│   │ Shopify │   │ Amazon  │   │ Flipkart  │   │ Meesho  │   │WooCommerce │        │
│   │   API   │   │ SP-API  │   │Seller API │   │   API   │   │ REST API   │        │
│   └─────────┘   └─────────┘   └───────────┘   └─────────┘   └────────────┘        │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Adapter Pattern Architecture

### Interface Definition

```csharp
// Application/Abstractions/Channels/IChannelAdapter.cs
public interface IChannelAdapter
{
    /// <summary>
    /// Channel type identifier
    /// </summary>
    ChannelType ChannelType { get; }

    // Connection Management
    Task<Result<bool>> TestConnectionAsync(CancellationToken ct = default);
    Task<Result<ChannelInfo>> GetChannelInfoAsync(CancellationToken ct = default);

    // Orders
    Task<Result<IReadOnlyList<UnifiedOrder>>> GetOrdersAsync(
        OrderSyncRequest request, CancellationToken ct = default);
    Task<Result<UnifiedOrder>> GetOrderAsync(
        string externalOrderId, CancellationToken ct = default);
    Task<Result<bool>> FulfillOrderAsync(
        string externalOrderId, FulfillmentRequest request, CancellationToken ct = default);
    Task<Result<bool>> CancelOrderAsync(
        string externalOrderId, string? reason = null, CancellationToken ct = default);

    // Products & Inventory
    Task<Result<IReadOnlyList<UnifiedProduct>>> GetProductsAsync(
        ProductSyncRequest request, CancellationToken ct = default);
    Task<Result<bool>> UpdateInventoryAsync(
        string externalProductId, string? variantId, int quantity, CancellationToken ct = default);
    Task<Result<bool>> UpdateInventoryBulkAsync(
        IReadOnlyList<InventoryUpdate> updates, CancellationToken ct = default);

    // Webhooks
    Task<Result<WebhookRegistration>> RegisterWebhooksAsync(
        IReadOnlyList<string> topics, string webhookUrl, CancellationToken ct = default);
    Task<Result<bool>> ValidateWebhookAsync(
        string payload, string signature, CancellationToken ct = default);
    Task<Result<WebhookEvent>> ProcessWebhookAsync(
        string payload, IDictionary<string, string> headers, CancellationToken ct = default);
}
```

### Factory Pattern

```csharp
// Integrations/Channels/Common/ChannelAdapterFactory.cs
public interface IChannelAdapterFactory
{
    IChannelAdapter CreateAdapter(SalesChannel channel);
    IChannelAdapter CreateAdapter(ChannelType type, ChannelCredentials credentials);
}

public class ChannelAdapterFactory : IChannelAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ChannelAdapterFactory> _logger;

    public ChannelAdapterFactory(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IEncryptionService encryptionService,
        ILogger<ChannelAdapterFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public IChannelAdapter CreateAdapter(SalesChannel channel)
    {
        var credentials = _encryptionService.Decrypt<ChannelCredentials>(channel.Credentials);
        var channelType = Enum.Parse<ChannelType>(channel.ChannelTypeCode, ignoreCase: true);

        return CreateAdapter(channelType, credentials);
    }

    public IChannelAdapter CreateAdapter(ChannelType type, ChannelCredentials credentials)
    {
        var httpClient = _httpClientFactory.CreateClient(type.ToString());

        return type switch
        {
            ChannelType.Shopify => new ShopifyAdapter(
                httpClient,
                _serviceProvider.GetRequiredService<ILogger<ShopifyAdapter>>(),
                credentials,
                _serviceProvider.GetRequiredService<ShopifyOrderMapper>(),
                _serviceProvider.GetRequiredService<ShopifyProductMapper>()),

            ChannelType.Amazon => new AmazonAdapter(
                httpClient,
                _serviceProvider.GetRequiredService<ILogger<AmazonAdapter>>(),
                credentials,
                _serviceProvider.GetRequiredService<AmazonSpApiClient>(),
                _serviceProvider.GetRequiredService<AmazonOrderMapper>()),

            ChannelType.Flipkart => new FlipkartAdapter(
                httpClient,
                _serviceProvider.GetRequiredService<ILogger<FlipkartAdapter>>(),
                credentials),

            ChannelType.Meesho => new MeeshoAdapter(
                httpClient,
                _serviceProvider.GetRequiredService<ILogger<MeeshoAdapter>>(),
                credentials),

            ChannelType.WooCommerce => new WooCommerceAdapter(
                httpClient,
                _serviceProvider.GetRequiredService<ILogger<WooCommerceAdapter>>(),
                credentials),

            _ => throw new NotSupportedException($"Channel type '{type}' is not supported")
        };
    }
}
```

---

## Channel Adapters

### Shopify Adapter

```csharp
// Integrations/Channels/Shopify/ShopifyAdapter.cs
public class ShopifyAdapter : BaseChannelAdapter
{
    private readonly ShopifyOrderMapper _orderMapper;
    private readonly ShopifyProductMapper _productMapper;

    public override ChannelType ChannelType => ChannelType.Shopify;

    public ShopifyAdapter(
        HttpClient httpClient,
        ILogger<ShopifyAdapter> logger,
        ChannelCredentials credentials,
        ShopifyOrderMapper orderMapper,
        ShopifyProductMapper productMapper) : base(httpClient, logger, credentials)
    {
        _orderMapper = orderMapper;
        _productMapper = productMapper;

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri($"https://{credentials.ShopDomain}/admin/api/2024-01/");
        _httpClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", credentials.AccessToken);
    }

    public override async Task<Result<bool>> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await GetAsync<ShopifyShopResponse>("shop.json", ct);
            return response.IsSuccess
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(response.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Shopify connection");
            return Result<bool>.Failure("CONNECTION_FAILED", ex.Message);
        }
    }

    public override async Task<Result<IReadOnlyList<UnifiedOrder>>> GetOrdersAsync(
        OrderSyncRequest request, CancellationToken ct = default)
    {
        try
        {
            var queryParams = BuildOrderQueryParams(request);
            var endpoint = $"orders.json?{queryParams}";

            var response = await GetAsync<ShopifyOrdersResponse>(endpoint, ct);
            if (!response.IsSuccess)
                return Result<IReadOnlyList<UnifiedOrder>>.Failure(response.Error!);

            var orders = response.Value!.Orders
                .Select(_orderMapper.MapToUnified)
                .ToList();

            return Result<IReadOnlyList<UnifiedOrder>>.Success(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Shopify orders");
            return Result<IReadOnlyList<UnifiedOrder>>.Failure("FETCH_FAILED", ex.Message);
        }
    }

    public override async Task<Result<bool>> UpdateInventoryAsync(
        string externalProductId,
        string? variantId,
        int quantity,
        CancellationToken ct = default)
    {
        try
        {
            // Get variant to find inventory item ID
            var targetId = variantId ?? externalProductId;
            var variantResponse = await GetAsync<ShopifyVariantResponse>(
                $"variants/{targetId}.json", ct);

            if (!variantResponse.IsSuccess)
                return Result<bool>.Failure(variantResponse.Error!);

            var inventoryItemId = variantResponse.Value!.Variant.InventoryItemId;

            // Get location (use primary location)
            var locationsResponse = await GetAsync<ShopifyLocationsResponse>(
                "locations.json", ct);

            var locationId = locationsResponse.Value!.Locations
                .First(l => l.Active)
                .Id;

            // Set inventory level
            var updateRequest = new
            {
                inventory_level = new
                {
                    location_id = locationId,
                    inventory_item_id = inventoryItemId,
                    available = quantity
                }
            };

            var updateResponse = await PostAsync<ShopifyInventoryLevelResponse>(
                "inventory_levels/set.json",
                updateRequest,
                ct);

            return updateResponse.IsSuccess
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(updateResponse.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Shopify inventory for {ProductId}", externalProductId);
            return Result<bool>.Failure("UPDATE_FAILED", ex.Message);
        }
    }

    public override async Task<Result<bool>> FulfillOrderAsync(
        string externalOrderId,
        FulfillmentRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // Get fulfillment order
            var fulfillmentOrdersResponse = await GetAsync<ShopifyFulfillmentOrdersResponse>(
                $"orders/{externalOrderId}/fulfillment_orders.json", ct);

            if (!fulfillmentOrdersResponse.IsSuccess)
                return Result<bool>.Failure(fulfillmentOrdersResponse.Error!);

            var fulfillmentOrder = fulfillmentOrdersResponse.Value!.FulfillmentOrders.First();

            // Create fulfillment
            var fulfillmentRequest = new
            {
                fulfillment = new
                {
                    line_items_by_fulfillment_order = new[]
                    {
                        new
                        {
                            fulfillment_order_id = fulfillmentOrder.Id,
                            fulfillment_order_line_items = fulfillmentOrder.LineItems
                                .Select(li => new { id = li.Id, quantity = li.Quantity })
                        }
                    },
                    tracking_info = new
                    {
                        company = request.CourierName,
                        number = request.AwbNumber,
                        url = request.TrackingUrl
                    },
                    notify_customer = request.NotifyCustomer
                }
            };

            var response = await PostAsync<ShopifyFulfillmentResponse>(
                "fulfillments.json",
                fulfillmentRequest,
                ct);

            return response.IsSuccess
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(response.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fulfill Shopify order {OrderId}", externalOrderId);
            return Result<bool>.Failure("FULFILLMENT_FAILED", ex.Message);
        }
    }

    public override async Task<Result<WebhookEvent>> ProcessWebhookAsync(
        string payload,
        IDictionary<string, string> headers,
        CancellationToken ct = default)
    {
        // Validate signature
        if (!headers.TryGetValue("X-Shopify-Hmac-SHA256", out var signature))
            return Result<WebhookEvent>.Failure("INVALID_WEBHOOK", "Missing signature");

        var isValid = ValidateHmacSignature(payload, signature);
        if (!isValid)
            return Result<WebhookEvent>.Failure("INVALID_SIGNATURE", "Signature mismatch");

        // Get topic
        if (!headers.TryGetValue("X-Shopify-Topic", out var topic))
            return Result<WebhookEvent>.Failure("INVALID_WEBHOOK", "Missing topic");

        var webhookEvent = new WebhookEvent
        {
            Topic = topic,
            Payload = payload,
            ReceivedAt = DateTime.UtcNow
        };

        // Parse based on topic
        switch (topic)
        {
            case "orders/create":
            case "orders/updated":
                var orderData = JsonSerializer.Deserialize<ShopifyOrder>(payload, _jsonOptions);
                webhookEvent.Data = _orderMapper.MapToUnified(orderData!);
                webhookEvent.EventType = WebhookEventType.OrderUpdated;
                break;

            case "orders/fulfilled":
                webhookEvent.EventType = WebhookEventType.OrderFulfilled;
                break;

            case "orders/cancelled":
                webhookEvent.EventType = WebhookEventType.OrderCancelled;
                break;

            case "inventory_levels/update":
                webhookEvent.EventType = WebhookEventType.InventoryUpdated;
                break;
        }

        return Result<WebhookEvent>.Success(webhookEvent);
    }

    private bool ValidateHmacSignature(string payload, string signature)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_credentials.WebhookSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var computedSignature = Convert.ToBase64String(hash);

        return computedSignature == signature;
    }
}
```

### Base Adapter Implementation

```csharp
// Integrations/Channels/Common/BaseChannelAdapter.cs
public abstract class BaseChannelAdapter : IChannelAdapter
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected readonly ChannelCredentials _credentials;
    protected readonly JsonSerializerOptions _jsonOptions;

    public abstract ChannelType ChannelType { get; }

    protected BaseChannelAdapter(
        HttpClient httpClient,
        ILogger logger,
        ChannelCredentials credentials)
    {
        _httpClient = httpClient;
        _logger = logger;
        _credentials = credentials;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    protected async Task<Result<T>> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, ct);
            return await HandleResponseAsync<T>(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET request failed: {Endpoint}", endpoint);
            return Result<T>.Failure("REQUEST_FAILED", ex.Message);
        }
    }

    protected async Task<Result<T>> PostAsync<T>(
        string endpoint, object body, CancellationToken ct = default)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(body, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, ct);
            return await HandleResponseAsync<T>(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST request failed: {Endpoint}", endpoint);
            return Result<T>.Failure("REQUEST_FAILED", ex.Message);
        }
    }

    private async Task<Result<T>> HandleResponseAsync<T>(
        HttpResponseMessage response, CancellationToken ct)
    {
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("API error: {Status} - {Content}",
                response.StatusCode, content);

            return Result<T>.Failure(
                "API_ERROR",
                $"API returned {(int)response.StatusCode}: {content}");
        }

        var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
        return Result<T>.Success(result!);
    }

    // Abstract methods to be implemented by each adapter
    public abstract Task<Result<bool>> TestConnectionAsync(CancellationToken ct = default);
    public abstract Task<Result<IReadOnlyList<UnifiedOrder>>> GetOrdersAsync(
        OrderSyncRequest request, CancellationToken ct = default);
    public abstract Task<Result<UnifiedOrder>> GetOrderAsync(
        string externalOrderId, CancellationToken ct = default);
    public abstract Task<Result<bool>> FulfillOrderAsync(
        string externalOrderId, FulfillmentRequest request, CancellationToken ct = default);
    public abstract Task<Result<bool>> CancelOrderAsync(
        string externalOrderId, string? reason = null, CancellationToken ct = default);
    public abstract Task<Result<IReadOnlyList<UnifiedProduct>>> GetProductsAsync(
        ProductSyncRequest request, CancellationToken ct = default);
    public abstract Task<Result<bool>> UpdateInventoryAsync(
        string externalProductId, string? variantId, int quantity, CancellationToken ct = default);
    public abstract Task<Result<bool>> UpdateInventoryBulkAsync(
        IReadOnlyList<InventoryUpdate> updates, CancellationToken ct = default);
    public abstract Task<Result<WebhookRegistration>> RegisterWebhooksAsync(
        IReadOnlyList<string> topics, string webhookUrl, CancellationToken ct = default);
    public abstract Task<Result<bool>> ValidateWebhookAsync(
        string payload, string signature, CancellationToken ct = default);
    public abstract Task<Result<WebhookEvent>> ProcessWebhookAsync(
        string payload, IDictionary<string, string> headers, CancellationToken ct = default);

    // Optional: default implementation returns empty
    public virtual Task<Result<ChannelInfo>> GetChannelInfoAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<ChannelInfo>.Failure("NOT_IMPLEMENTED", "Not implemented"));
    }
}
```

---

## Courier Adapters

### Courier Adapter Interface

```csharp
// Application/Abstractions/Couriers/ICourierAdapter.cs
public interface ICourierAdapter
{
    CourierType CourierType { get; }

    // Connection
    Task<Result<bool>> TestConnectionAsync(CancellationToken ct = default);
    Task<Result<CourierAccountInfo>> GetAccountInfoAsync(CancellationToken ct = default);

    // Shipment Operations
    Task<Result<ShipmentResponse>> CreateShipmentAsync(
        ShipmentRequest request, CancellationToken ct = default);
    Task<Result<ShipmentResponse>> CreateShipmentBulkAsync(
        IReadOnlyList<ShipmentRequest> requests, CancellationToken ct = default);
    Task<Result<bool>> CancelShipmentAsync(
        string awbNumber, CancellationToken ct = default);

    // AWB & Labels
    Task<Result<AwbResponse>> GenerateAwbAsync(
        string shipmentId, CancellationToken ct = default);
    Task<Result<LabelResponse>> GetLabelAsync(
        string awbNumber, CancellationToken ct = default);
    Task<Result<ManifestResponse>> GenerateManifestAsync(
        IReadOnlyList<string> awbNumbers, CancellationToken ct = default);

    // Tracking
    Task<Result<TrackingResponse>> GetTrackingAsync(
        string awbNumber, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TrackingResponse>>> GetTrackingBulkAsync(
        IReadOnlyList<string> awbNumbers, CancellationToken ct = default);

    // Webhooks
    Task<Result<WebhookEvent>> ProcessWebhookAsync(
        string payload, IDictionary<string, string> headers, CancellationToken ct = default);

    // Serviceability
    Task<Result<ServiceabilityResponse>> CheckServiceabilityAsync(
        string pickupPincode, string deliveryPincode, CancellationToken ct = default);
    Task<Result<RateResponse>> GetRatesAsync(
        RateRequest request, CancellationToken ct = default);
}
```

### Shiprocket Adapter

```csharp
// Integrations/Couriers/Shiprocket/ShiprocketAdapter.cs
public class ShiprocketAdapter : BaseCourierAdapter
{
    private readonly ShiprocketApiClient _apiClient;
    private string? _authToken;

    public override CourierType CourierType => CourierType.Shiprocket;

    public ShiprocketAdapter(
        HttpClient httpClient,
        ILogger<ShiprocketAdapter> logger,
        CourierCredentials credentials,
        ShiprocketApiClient apiClient) : base(httpClient, logger, credentials)
    {
        _apiClient = apiClient;
        _httpClient.BaseAddress = new Uri("https://apiv2.shiprocket.in/v1/external/");
    }

    public override async Task<Result<bool>> TestConnectionAsync(CancellationToken ct = default)
    {
        var tokenResult = await EnsureAuthenticatedAsync(ct);
        return tokenResult.IsSuccess
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(tokenResult.Error!);
    }

    public override async Task<Result<ShipmentResponse>> CreateShipmentAsync(
        ShipmentRequest request, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(ct);

            // First, create order in Shiprocket
            var orderRequest = MapToShiprocketOrder(request);
            var orderResponse = await PostAsync<ShiprocketOrderResponse>(
                "orders/create/adhoc",
                orderRequest,
                ct);

            if (!orderResponse.IsSuccess)
                return Result<ShipmentResponse>.Failure(orderResponse.Error!);

            // Generate AWB
            var awbRequest = new
            {
                shipment_id = orderResponse.Value!.ShipmentId,
                courier_id = request.PreferredCourierId
            };

            var awbResponse = await PostAsync<ShiprocketAwbResponse>(
                "courier/assign/awb",
                awbRequest,
                ct);

            if (!awbResponse.IsSuccess)
                return Result<ShipmentResponse>.Failure(awbResponse.Error!);

            return Result<ShipmentResponse>.Success(new ShipmentResponse
            {
                ExternalShipmentId = orderResponse.Value.ShipmentId.ToString(),
                AwbNumber = awbResponse.Value!.AwbCode,
                CourierName = awbResponse.Value.CourierName,
                TrackingUrl = $"https://shiprocket.co/tracking/{awbResponse.Value.AwbCode}",
                Status = ShipmentStatus.Created
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Shiprocket shipment");
            return Result<ShipmentResponse>.Failure("CREATE_FAILED", ex.Message);
        }
    }

    public override async Task<Result<TrackingResponse>> GetTrackingAsync(
        string awbNumber, CancellationToken ct = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(ct);

            var response = await GetAsync<ShiprocketTrackingResponse>(
                $"courier/track/awb/{awbNumber}",
                ct);

            if (!response.IsSuccess)
                return Result<TrackingResponse>.Failure(response.Error!);

            var tracking = response.Value!.TrackingData;
            return Result<TrackingResponse>.Success(new TrackingResponse
            {
                AwbNumber = awbNumber,
                CurrentStatus = MapStatus(tracking.CurrentStatus),
                CurrentStatusCode = tracking.CurrentStatusCode,
                ExpectedDeliveryDate = tracking.Etd,
                Events = tracking.ShipmentTrack?
                    .Select(e => new TrackingEvent
                    {
                        Status = e.Activity,
                        Location = e.Location,
                        DateTime = e.Date,
                        Description = e.Activity
                    })
                    .ToList() ?? new List<TrackingEvent>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tracking for AWB {AwbNumber}", awbNumber);
            return Result<TrackingResponse>.Failure("TRACKING_FAILED", ex.Message);
        }
    }

    public override async Task<Result<WebhookEvent>> ProcessWebhookAsync(
        string payload,
        IDictionary<string, string> headers,
        CancellationToken ct = default)
    {
        try
        {
            var webhookData = JsonSerializer.Deserialize<ShiprocketWebhookPayload>(
                payload, _jsonOptions);

            var webhookEvent = new WebhookEvent
            {
                Topic = webhookData!.CurrentStatus,
                Payload = payload,
                ReceivedAt = DateTime.UtcNow
            };

            // Map Shiprocket status to internal status
            var (eventType, shipmentStatus) = MapWebhookStatus(webhookData.CurrentStatusId);

            webhookEvent.EventType = eventType;
            webhookEvent.Data = new ShipmentStatusUpdate
            {
                AwbNumber = webhookData.Awb,
                Status = shipmentStatus,
                StatusCode = webhookData.CurrentStatusId.ToString(),
                Location = webhookData.CurrentCity,
                DateTime = webhookData.StatusDate,
                NdrReason = webhookData.NdrReason,
                RtoReason = webhookData.RtoReason
            };

            return Result<WebhookEvent>.Success(webhookEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Shiprocket webhook");
            return Result<WebhookEvent>.Failure("WEBHOOK_FAILED", ex.Message);
        }
    }

    private async Task<Result<string>> EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_authToken))
            return Result<string>.Success(_authToken);

        var authResponse = await _apiClient.AuthenticateAsync(
            _credentials.Email,
            _credentials.Password,
            ct);

        if (!authResponse.IsSuccess)
            return Result<string>.Failure(authResponse.Error!);

        _authToken = authResponse.Value!.Token;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _authToken);

        return Result<string>.Success(_authToken);
    }

    private (WebhookEventType, ShipmentStatus) MapWebhookStatus(int statusId)
    {
        return statusId switch
        {
            6 => (WebhookEventType.ShipmentPickedUp, ShipmentStatus.Picked),
            17 or 18 => (WebhookEventType.ShipmentInTransit, ShipmentStatus.InTransit),
            19 => (WebhookEventType.ShipmentOutForDelivery, ShipmentStatus.OutForDelivery),
            7 => (WebhookEventType.ShipmentDelivered, ShipmentStatus.Delivered),
            21 or 22 => (WebhookEventType.NdrReceived, ShipmentStatus.NdrPending),
            9 or 10 => (WebhookEventType.RtoInitiated, ShipmentStatus.RtoInitiated),
            14 => (WebhookEventType.RtoDelivered, ShipmentStatus.RtoDelivered),
            8 => (WebhookEventType.ShipmentCancelled, ShipmentStatus.Cancelled),
            _ => (WebhookEventType.StatusUpdated, ShipmentStatus.InTransit)
        };
    }
}
```

---

## Unified Data Models

### Unified Order Model

```csharp
// Application/Abstractions/Channels/UnifiedModels/UnifiedOrder.cs
public class UnifiedOrder
{
    public string ExternalOrderId { get; set; } = string.Empty;
    public string ExternalOrderNumber { get; set; } = string.Empty;
    public ChannelType SourceChannel { get; set; }

    // Status
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public FulfillmentStatus FulfillmentStatus { get; set; }

    // Customer
    public CustomerInfo Customer { get; set; } = new();

    // Addresses
    public Address ShippingAddress { get; set; } = new();
    public Address? BillingAddress { get; set; }

    // Financial
    public Money Subtotal { get; set; } = Money.Zero;
    public Money DiscountAmount { get; set; } = Money.Zero;
    public Money TaxAmount { get; set; } = Money.Zero;
    public Money ShippingAmount { get; set; } = Money.Zero;
    public Money TotalAmount { get; set; } = Money.Zero;

    // Payment
    public PaymentMethod? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }

    // Dates
    public DateTime OrderDate { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Items
    public IReadOnlyList<UnifiedOrderItem> Items { get; set; } = Array.Empty<UnifiedOrderItem>();

    // Metadata (channel-specific data)
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class UnifiedOrderItem
{
    public string? ExternalProductId { get; set; }
    public string? ExternalVariantId { get; set; }
    public string? Sku { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Money UnitPrice { get; set; } = Money.Zero;
    public Money DiscountAmount { get; set; } = Money.Zero;
    public Money TaxAmount { get; set; } = Money.Zero;
    public Money TotalAmount { get; set; } = Money.Zero;
    public int FulfilledQuantity { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### Order Mapper (Shopify Example)

```csharp
// Integrations/Channels/Shopify/Mappers/ShopifyOrderMapper.cs
public class ShopifyOrderMapper
{
    public UnifiedOrder MapToUnified(ShopifyOrder shopifyOrder)
    {
        return new UnifiedOrder
        {
            ExternalOrderId = shopifyOrder.Id.ToString(),
            ExternalOrderNumber = shopifyOrder.OrderNumber.ToString(),
            SourceChannel = ChannelType.Shopify,

            Status = MapOrderStatus(shopifyOrder),
            PaymentStatus = MapPaymentStatus(shopifyOrder.FinancialStatus),
            FulfillmentStatus = MapFulfillmentStatus(shopifyOrder.FulfillmentStatus),

            Customer = new CustomerInfo
            {
                Name = shopifyOrder.Customer != null
                    ? $"{shopifyOrder.Customer.FirstName} {shopifyOrder.Customer.LastName}".Trim()
                    : shopifyOrder.ShippingAddress?.Name ?? "Unknown",
                Email = shopifyOrder.Customer?.Email ?? shopifyOrder.Email,
                Phone = shopifyOrder.Customer?.Phone ?? shopifyOrder.Phone
            },

            ShippingAddress = MapAddress(shopifyOrder.ShippingAddress),
            BillingAddress = shopifyOrder.BillingAddress != null
                ? MapAddress(shopifyOrder.BillingAddress)
                : null,

            Subtotal = new Money(shopifyOrder.SubtotalPrice, shopifyOrder.Currency),
            DiscountAmount = new Money(shopifyOrder.TotalDiscounts, shopifyOrder.Currency),
            TaxAmount = new Money(shopifyOrder.TotalTax, shopifyOrder.Currency),
            ShippingAmount = new Money(
                shopifyOrder.ShippingLines?.Sum(s => s.Price) ?? 0,
                shopifyOrder.Currency),
            TotalAmount = new Money(shopifyOrder.TotalPrice, shopifyOrder.Currency),

            PaymentMethod = MapPaymentMethod(shopifyOrder.PaymentGatewayNames?.FirstOrDefault()),
            PaymentReference = shopifyOrder.TransactionId,

            OrderDate = shopifyOrder.CreatedAt,
            ConfirmedAt = shopifyOrder.ConfirmedAt,

            Items = shopifyOrder.LineItems.Select(MapOrderItem).ToList(),

            Metadata = new Dictionary<string, object>
            {
                ["shopify_id"] = shopifyOrder.Id,
                ["note"] = shopifyOrder.Note ?? string.Empty,
                ["tags"] = shopifyOrder.Tags ?? string.Empty,
                ["source_name"] = shopifyOrder.SourceName ?? string.Empty
            }
        };
    }

    private UnifiedOrderItem MapOrderItem(ShopifyLineItem item)
    {
        return new UnifiedOrderItem
        {
            ExternalProductId = item.ProductId?.ToString(),
            ExternalVariantId = item.VariantId?.ToString(),
            Sku = item.Sku,
            Name = item.Name,
            Quantity = item.Quantity,
            UnitPrice = new Money(item.Price, "INR"),
            DiscountAmount = new Money(item.TotalDiscount, "INR"),
            TaxAmount = new Money(item.TaxLines?.Sum(t => t.Price) ?? 0, "INR"),
            TotalAmount = new Money(
                (item.Price * item.Quantity) - item.TotalDiscount,
                "INR"),
            FulfilledQuantity = item.FulfillableQuantity ?? 0
        };
    }

    private OrderStatus MapOrderStatus(ShopifyOrder order)
    {
        if (order.CancelledAt.HasValue)
            return OrderStatus.Cancelled;

        return order.FulfillmentStatus?.ToLower() switch
        {
            null or "null" => OrderStatus.Pending,
            "partial" => OrderStatus.Processing,
            "fulfilled" => OrderStatus.Shipped,
            _ => OrderStatus.Pending
        };
    }

    private PaymentStatus MapPaymentStatus(string? financialStatus)
    {
        return financialStatus?.ToLower() switch
        {
            "paid" => PaymentStatus.Paid,
            "pending" => PaymentStatus.Pending,
            "refunded" => PaymentStatus.Refunded,
            "partially_refunded" => PaymentStatus.PartiallyRefunded,
            "voided" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
    }

    private Address MapAddress(ShopifyAddress? address)
    {
        if (address == null)
            return new Address();

        return new Address
        {
            Name = address.Name ?? $"{address.FirstName} {address.LastName}".Trim(),
            Phone = address.Phone,
            Line1 = address.Address1,
            Line2 = address.Address2,
            City = address.City,
            State = address.Province,
            PostalCode = address.Zip,
            Country = address.CountryCode ?? "IN"
        };
    }
}
```

---

## Webhook Processing

### Webhook Controller

```csharp
// API/Controllers/V1/WebhooksController.cs
[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebhooksController> _logger;

    [HttpPost("channels/{channelType}/{channelId}")]
    public async Task<IActionResult> HandleChannelWebhook(
        string channelType,
        Guid channelId,
        CancellationToken ct)
    {
        var payload = await ReadBodyAsync();
        var headers = GetHeaders();

        var command = new ProcessChannelWebhookCommand
        {
            ChannelType = channelType,
            ChannelId = channelId,
            Payload = payload,
            Headers = headers
        };

        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Webhook processing failed: {Error}", result.Error?.Message);
            return BadRequest(result.Error);
        }

        return Ok();
    }

    [HttpPost("couriers/{courierType}")]
    public async Task<IActionResult> HandleCourierWebhook(
        string courierType,
        CancellationToken ct)
    {
        var payload = await ReadBodyAsync();
        var headers = GetHeaders();

        var command = new ProcessCourierWebhookCommand
        {
            CourierType = courierType,
            Payload = payload,
            Headers = headers
        };

        var result = await _mediator.Send(command, ct);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Courier webhook processing failed: {Error}", result.Error?.Message);
            return BadRequest(result.Error);
        }

        return Ok();
    }

    private async Task<string> ReadBodyAsync()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private Dictionary<string, string> GetHeaders()
    {
        return Request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.ToString());
    }
}
```

### Webhook Command Handler

```csharp
// Application/Features/Channels/Commands/ProcessChannelWebhook/ProcessChannelWebhookCommandHandler.cs
public class ProcessChannelWebhookCommandHandler
    : IRequestHandler<ProcessChannelWebhookCommand, Result<bool>>
{
    private readonly ITenantDbContext _context;
    private readonly IChannelAdapterFactory _adapterFactory;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessChannelWebhookCommandHandler> _logger;

    public async Task<Result<bool>> Handle(
        ProcessChannelWebhookCommand request,
        CancellationToken ct)
    {
        // Get channel
        var channel = await _context.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId, ct);

        if (channel == null)
            return Result<bool>.Failure("CHANNEL_NOT_FOUND", "Channel not found");

        // Create adapter and process webhook
        var adapter = _adapterFactory.CreateAdapter(channel);
        var result = await adapter.ProcessWebhookAsync(request.Payload, request.Headers, ct);

        if (!result.IsSuccess)
            return Result<bool>.Failure(result.Error!);

        var webhookEvent = result.Value!;

        // Handle different event types
        switch (webhookEvent.EventType)
        {
            case WebhookEventType.OrderCreated:
            case WebhookEventType.OrderUpdated:
                await HandleOrderEventAsync(webhookEvent, channel, ct);
                break;

            case WebhookEventType.OrderCancelled:
                await HandleOrderCancelledAsync(webhookEvent, channel, ct);
                break;

            case WebhookEventType.InventoryUpdated:
                await HandleInventoryUpdateAsync(webhookEvent, channel, ct);
                break;
        }

        return Result<bool>.Success(true);
    }

    private async Task HandleOrderEventAsync(
        WebhookEvent webhookEvent,
        SalesChannel channel,
        CancellationToken ct)
    {
        var unifiedOrder = (UnifiedOrder)webhookEvent.Data!;

        // Check if order exists
        var existingOrder = await _context.Orders
            .FirstOrDefaultAsync(o =>
                o.ChannelId == channel.Id &&
                o.ExternalOrderId == unifiedOrder.ExternalOrderId, ct);

        if (existingOrder == null)
        {
            // Create new order
            await _mediator.Send(new CreateOrderFromChannelCommand
            {
                ChannelId = channel.Id,
                UnifiedOrder = unifiedOrder
            }, ct);
        }
        else
        {
            // Update existing order
            await _mediator.Send(new UpdateOrderFromChannelCommand
            {
                OrderId = existingOrder.Id,
                UnifiedOrder = unifiedOrder
            }, ct);
        }
    }
}
```

---

## Sync Strategies

### Order Sync Background Job

```csharp
// Infrastructure/BackgroundJobs/Jobs/OrderSyncJob.cs
public class OrderSyncJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderSyncJob> _logger;

    [Queue("sync")]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task SyncOrdersForChannelAsync(
        Guid tenantId,
        Guid channelId,
        DateTime? sinceDate = null)
    {
        using var scope = _scopeFactory.CreateScope();

        var tenantService = scope.ServiceProvider.GetRequiredService<ICurrentTenantService>();
        await tenantService.SetTenantAsync(tenantId);

        var context = scope.ServiceProvider.GetRequiredService<ITenantDbContext>();
        var adapterFactory = scope.ServiceProvider.GetRequiredService<IChannelAdapterFactory>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var channel = await context.SalesChannels
            .FirstOrDefaultAsync(c => c.Id == channelId);

        if (channel == null || channel.Status != "active")
        {
            _logger.LogWarning("Channel {ChannelId} not found or inactive", channelId);
            return;
        }

        var adapter = adapterFactory.CreateAdapter(channel);

        // Sync orders in batches
        var syncRequest = new OrderSyncRequest
        {
            UpdatedAtMin = sinceDate ?? channel.LastSyncAt ?? DateTime.UtcNow.AddDays(-7),
            Status = "any",
            Limit = 50
        };

        var hasMore = true;
        var totalSynced = 0;

        while (hasMore)
        {
            var result = await adapter.GetOrdersAsync(syncRequest);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to sync orders: {Error}", result.Error?.Message);

                // Update channel with error
                channel.SyncError = result.Error?.Message;
                await context.SaveChangesAsync();

                throw new Exception($"Order sync failed: {result.Error?.Message}");
            }

            var orders = result.Value!;
            hasMore = orders.Count == syncRequest.Limit;

            foreach (var unifiedOrder in orders)
            {
                try
                {
                    await mediator.Send(new UpsertOrderFromSyncCommand
                    {
                        ChannelId = channel.Id,
                        UnifiedOrder = unifiedOrder
                    });

                    totalSynced++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync order {OrderId}",
                        unifiedOrder.ExternalOrderId);
                }
            }

            // Get next page
            if (hasMore && orders.Any())
            {
                syncRequest.SinceId = orders.Last().ExternalOrderId;
            }
        }

        // Update channel sync status
        channel.LastSyncAt = DateTime.UtcNow;
        channel.SyncError = null;
        await context.SaveChangesAsync();

        _logger.LogInformation("Synced {Count} orders for channel {ChannelId}",
            totalSynced, channelId);
    }
}
```

---

## Error Handling

### Retry Policy with Polly

```csharp
// Integrations/DependencyInjection.cs
public static IServiceCollection AddIntegrations(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Shopify HTTP client with retry
    services.AddHttpClient("Shopify")
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

    // Shiprocket HTTP client
    services.AddHttpClient("Shiprocket")
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

    // ... other registrations

    return services;
}

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: (retryAttempt, response, context) =>
            {
                // Check for Retry-After header
                if (response.Result?.Headers.RetryAfter?.Delta != null)
                {
                    return response.Result.Headers.RetryAfter.Delta.Value;
                }

                // Exponential backoff
                return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
            },
            onRetryAsync: (outcome, timespan, retryAttempt, context) =>
            {
                // Log retry
                return Task.CompletedTask;
            });
}

private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(1));
}
```

---

## Next Steps

See the following documents for more details:
- [NDR Workflow](07-ndr-workflow.md)
- [API Design](08-api-design.md)
- [Development Roadmap](10-development-roadmap.md)
