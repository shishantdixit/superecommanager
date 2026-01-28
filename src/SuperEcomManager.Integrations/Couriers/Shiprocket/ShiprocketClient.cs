using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperEcomManager.Integrations.Couriers.Shiprocket.Models;

namespace SuperEcomManager.Integrations.Couriers.Shiprocket;

/// <summary>
/// HTTP client implementation for Shiprocket API.
/// </summary>
public class ShiprocketClient : IShiprocketClient
{
    private readonly HttpClient _httpClient;
    private readonly ShiprocketSettings _settings;
    private readonly ILogger<ShiprocketClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ShiprocketClient(
        HttpClient httpClient,
        IOptions<ShiprocketSettings> settings,
        ILogger<ShiprocketClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // BaseAddress MUST end with '/' for correct relative URI resolution.
        // Without trailing slash, the last path segment is dropped by .NET URI resolution.
        var baseUrl = _settings.BaseUrl.TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<ShiprocketAuthResponse?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestUrl = new Uri(_httpClient.BaseAddress!, "auth/login");
            _logger.LogInformation("Shiprocket auth request URL: {Url} for user {Email}", requestUrl, email);

            var request = new ShiprocketAuthRequest { Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync("auth/login", request, JsonOptions, cancellationToken);

            _logger.LogInformation("Shiprocket auth response status: {StatusCode}", response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Shiprocket auth failed ({StatusCode}): {Error}", response.StatusCode, responseBody);
                return null;
            }

            var authResponse = JsonSerializer.Deserialize<ShiprocketAuthResponse>(responseBody, JsonOptions);

            if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
            {
                _logger.LogError("Shiprocket auth response OK but token is empty. Response body: {Body}", responseBody);
            }

            return authResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket authentication error");
            throw;
        }
    }

    public async Task<ShiprocketCreateOrderResponse?> CreateOrderAsync(
        string token,
        ShiprocketCreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "orders/create/adhoc");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket create order failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketCreateOrderResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket create order error");
            throw;
        }
    }

    public async Task<ShiprocketCreateOrderResponse?> CreateChannelOrderAsync(
        string token,
        ShiprocketCreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "orders/create");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket create channel order failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketCreateOrderResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket create channel order error");
            throw;
        }
    }

    public async Task<ShiprocketAwbResponse?> GenerateAwbAsync(
        string token,
        ShiprocketGenerateAwbRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "courier/assign/awb");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket AWB generation failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketAwbResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket AWB generation error");
            throw;
        }
    }

    public async Task<ShiprocketServiceabilityResponse?> CheckServiceabilityAsync(
        string token,
        string pickupPincode,
        string deliveryPincode,
        decimal weight,
        bool isCod,
        long? orderId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"courier/serviceability/?pickup_postcode={pickupPincode}&delivery_postcode={deliveryPincode}&weight={weight}&cod={( isCod ? 1 : 0 )}";

            if (orderId.HasValue)
                url += $"&order_id={orderId}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket serviceability check failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketServiceabilityResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket serviceability check error");
            throw;
        }
    }

    public async Task<ShiprocketTrackingResponse?> GetTrackingAsync(
        string token,
        string awbCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"courier/track/awb/{awbCode}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket tracking failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketTrackingResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket tracking error");
            throw;
        }
    }

    public async Task<ShiprocketPickupResponse?> SchedulePickupAsync(
        string token,
        List<long> shipmentIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new ShiprocketPickupRequest { ShipmentIds = shipmentIds };

            using var request = new HttpRequestMessage(HttpMethod.Post, "courier/generate/pickup");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket pickup scheduling failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketPickupResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket pickup scheduling error");
            throw;
        }
    }

    public async Task<ShiprocketCancelResponse?> CancelShipmentAsync(
        string token,
        List<long> orderIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new ShiprocketCancelRequest { Ids = orderIds };

            using var request = new HttpRequestMessage(HttpMethod.Post, "orders/cancel");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket cancel failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketCancelResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket cancel error");
            throw;
        }
    }

    public async Task<ShiprocketLabelResponse?> GetLabelAsync(
        string token,
        List<long> shipmentIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var idsParam = string.Join(",", shipmentIds);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"courier/generate/label?shipment_id={idsParam}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket label generation failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketLabelResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket label generation error");
            throw;
        }
    }

    public async Task<byte[]?> DownloadLabelAsync(
        string labelUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(labelUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to download label from {Url}", labelUrl);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading label");
            throw;
        }
    }

    // ========== ORDERS MANAGEMENT ==========

    public async Task<ShiprocketOrdersResponse?> GetOrdersAsync(
        string token,
        int page = 1,
        int perPage = 10,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"orders?page={page}&per_page={perPage}";
            if (!string.IsNullOrEmpty(filter))
                url += $"&filter={filter}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get orders failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketOrdersResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get orders error");
            throw;
        }
    }

    public async Task<ShiprocketOrderDetailResponse?> GetOrderByIdAsync(
        string token,
        long orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"orders/show/{orderId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get order by ID failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketOrderDetailResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get order by ID error");
            throw;
        }
    }

    public async Task<ShiprocketUpdateOrderResponse?> UpdateOrderAsync(
        string token,
        long orderId,
        ShiprocketUpdateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"orders/update/{orderId}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket update order failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketUpdateOrderResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket update order error");
            throw;
        }
    }

    // ========== SHIPMENTS MANAGEMENT ==========

    public async Task<ShiprocketShipmentsResponse?> GetShipmentsAsync(
        string token,
        int page = 1,
        int perPage = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"shipments?page={page}&per_page={perPage}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get shipments failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketShipmentsResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get shipments error");
            throw;
        }
    }

    public async Task<ShiprocketShipmentDetailResponse?> GetShipmentByIdAsync(
        string token,
        long shipmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"shipments/show/{shipmentId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get shipment by ID failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketShipmentDetailResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get shipment by ID error");
            throw;
        }
    }

    // ========== RETURNS MANAGEMENT ==========

    public async Task<ShiprocketReturnResponse?> CreateReturnAsync(
        string token,
        ShiprocketCreateReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "orders/create/return");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket create return failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketReturnResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket create return error");
            throw;
        }
    }

    public async Task<ShiprocketReturnsListResponse?> GetReturnsAsync(
        string token,
        int page = 1,
        int perPage = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"orders/processing/return?page={page}&per_page={perPage}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get returns failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketReturnsListResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get returns error");
            throw;
        }
    }

    // ========== MANIFEST MANAGEMENT ==========

    public async Task<ShiprocketManifestResponse?> GenerateManifestAsync(
        string token,
        List<long> shipmentIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new ShiprocketManifestRequest { ShipmentIds = shipmentIds };

            using var request = new HttpRequestMessage(HttpMethod.Post, "manifests/generate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket generate manifest failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketManifestResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket generate manifest error");
            throw;
        }
    }

    public async Task<ShiprocketPrintManifestResponse?> PrintManifestAsync(
        string token,
        List<long> orderIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new ShiprocketPrintManifestRequest { OrderIds = orderIds };

            using var request = new HttpRequestMessage(HttpMethod.Post, "manifests/print");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket print manifest failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketPrintManifestResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket print manifest error");
            throw;
        }
    }

    // ========== PICKUP MANAGEMENT ==========

    public async Task<ShiprocketCancelPickupResponse?> CancelPickupAsync(
        string token,
        List<long> shipmentIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new ShiprocketCancelPickupRequest { ShipmentIds = shipmentIds };

            using var request = new HttpRequestMessage(HttpMethod.Post, "courier/cancel/pickup");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket cancel pickup failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketCancelPickupResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket cancel pickup error");
            throw;
        }
    }

    public async Task<ShiprocketPickupLocationsResponse?> GetPickupLocationsAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "settings/company/pickup");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get pickup locations failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketPickupLocationsResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get pickup locations error");
            throw;
        }
    }

    // ========== WALLET MANAGEMENT ==========

    public async Task<ShiprocketWalletResponse?> GetWalletBalanceAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "account/details/wallet-balance");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("Shiprocket wallet balance raw response: {Body}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Shiprocket get wallet balance failed ({StatusCode}): {Error}", response.StatusCode, responseBody);
                return null;
            }

            var walletResponse = JsonSerializer.Deserialize<ShiprocketWalletResponse>(responseBody, JsonOptions);
            _logger.LogInformation("Shiprocket wallet balance deserialized: Balance={Balance}",
                walletResponse?.Data?.Balance);

            return walletResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get wallet balance error");
            throw;
        }
    }

    // ========== CHANNELS MANAGEMENT ==========

    public async Task<ShiprocketChannelsResponse?> GetChannelsAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "channels");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get channels failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketChannelsResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get channels error");
            throw;
        }
    }

    public async Task<ShiprocketChannelResponse?> CreateChannelAsync(
        string token,
        ShiprocketCreateChannelRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "channels/create");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket create channel failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketChannelResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket create channel error");
            throw;
        }
    }

    // ========== INVENTORY MANAGEMENT ==========

    public async Task<ShiprocketProductsResponse?> GetProductsAsync(
        string token,
        int page = 1,
        int perPage = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"products?page={page}&per_page={perPage}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get products failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketProductsResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get products error");
            throw;
        }
    }

    public async Task<ShiprocketAddProductResponse?> AddProductAsync(
        string token,
        ShiprocketAddProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "products/add");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket add product failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketAddProductResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket add product error");
            throw;
        }
    }

    public async Task<ShiprocketUpdateInventoryResponse?> UpdateInventoryAsync(
        string token,
        ShiprocketUpdateInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "products/inventory/update");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket update inventory failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketUpdateInventoryResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket update inventory error");
            throw;
        }
    }

    // ========== COURIER PARTNERS ==========

    public async Task<ShiprocketCourierPartnersResponse?> GetCourierPartnersAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "courier");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get courier partners failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketCourierPartnersResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get courier partners error");
            throw;
        }
    }

    // ========== NDR MANAGEMENT ==========

    public async Task<ShiprocketNdrResponse?> UpdateNdrActionAsync(
        string token,
        ShiprocketNdrActionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "courier/track/ndr-action");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket update NDR action failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketNdrResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket update NDR action error");
            throw;
        }
    }

    public async Task<ShiprocketNdrListResponse?> GetNdrListAsync(
        string token,
        int page = 1,
        int perPage = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"courier/track/ndr?page={page}&per_page={perPage}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get NDR list failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketNdrListResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get NDR list error");
            throw;
        }
    }

    // ========== WEIGHT RECONCILIATION ==========

    public async Task<ShiprocketWeightDisputesResponse?> GetWeightDisputesAsync(
        string token,
        int page = 1,
        int perPage = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"courier/discrepancy/weight?page={page}&per_page={perPage}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket get weight disputes failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketWeightDisputesResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get weight disputes error");
            throw;
        }
    }

    // ========== WEBHOOKS MANAGEMENT ==========

    public async Task<ShiprocketWebhookResponse?> CreateWebhookAsync(
        string token,
        ShiprocketCreateWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "webhooks/create");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket create webhook failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketWebhookResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket create webhook error");
            throw;
        }
    }

    public async Task<ShiprocketWebhookResponse?> UpdateWebhookAsync(
        string token,
        long webhookId,
        ShiprocketUpdateWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"webhooks/{webhookId}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket update webhook failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketWebhookResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket update webhook error");
            throw;
        }
    }

    public async Task<ShiprocketDeleteWebhookResponse?> DeleteWebhookAsync(
        string token,
        long webhookId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"webhooks/{webhookId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket delete webhook failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketDeleteWebhookResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket delete webhook error");
            throw;
        }
    }
}
