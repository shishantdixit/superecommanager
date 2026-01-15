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

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
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
            var request = new ShiprocketAuthRequest { Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync("/auth/login", request, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Shiprocket auth failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ShiprocketAuthResponse>(JsonOptions, cancellationToken);
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
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/orders/create/adhoc");
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

    public async Task<ShiprocketAwbResponse?> GenerateAwbAsync(
        string token,
        ShiprocketGenerateAwbRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/courier/assign/awb");
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
            var url = $"/courier/serviceability/?pickup_postcode={pickupPincode}&delivery_postcode={deliveryPincode}&weight={weight}&cod={( isCod ? 1 : 0 )}";

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
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/courier/track/awb/{awbCode}");
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

            using var request = new HttpRequestMessage(HttpMethod.Post, "/courier/generate/pickup");
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

            using var request = new HttpRequestMessage(HttpMethod.Post, "/orders/cancel");
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

            using var request = new HttpRequestMessage(HttpMethod.Get, $"/courier/generate/label?shipment_id={idsParam}");
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
}
