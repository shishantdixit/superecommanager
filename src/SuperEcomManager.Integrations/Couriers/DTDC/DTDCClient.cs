using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperEcomManager.Integrations.Couriers.DTDC.Models;

namespace SuperEcomManager.Integrations.Couriers.DTDC;

/// <summary>
/// HTTP client implementation for DTDC API.
/// </summary>
public class DTDCClient : IDTDCClient
{
    private readonly HttpClient _httpClient;
    private readonly DTDCSettings _settings;
    private readonly ILogger<DTDCClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DTDCClient(
        HttpClient httpClient,
        IOptions<DTDCSettings> settings,
        ILogger<DTDCClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<DTDCCreateShipmentResponse?> CreateShipmentAsync(
        string apiKey,
        DTDCCreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shipment/create");
            httpRequest.Headers.Add("X-API-Key", apiKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("DTDC create shipment failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DTDCCreateShipmentResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC create shipment error");
            throw;
        }
    }

    public async Task<DTDCPincodeResponse?> CheckPincodeServiceabilityAsync(
        string apiKey,
        string pincode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/pincode/{pincode}");
            request.Headers.Add("X-API-Key", apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("DTDC pincode check failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DTDCPincodeResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC pincode check error");
            throw;
        }
    }

    public async Task<DTDCTrackingResponse?> GetTrackingAsync(
        string apiKey,
        string consignmentNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/tracking/{consignmentNumber}");
            request.Headers.Add("X-API-Key", apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("DTDC tracking failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DTDCTrackingResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC tracking error");
            throw;
        }
    }

    public async Task<DTDCPickupResponse?> SchedulePickupAsync(
        string apiKey,
        DTDCPickupRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pickup/schedule");
            httpRequest.Headers.Add("X-API-Key", apiKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("DTDC pickup scheduling failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DTDCPickupResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC pickup scheduling error");
            throw;
        }
    }

    public async Task<DTDCCancelResponse?> CancelShipmentAsync(
        string apiKey,
        DTDCCancelRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/shipment/cancel");
            httpRequest.Headers.Add("X-API-Key", apiKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("DTDC cancellation failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DTDCCancelResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC cancellation error");
            throw;
        }
    }

    public async Task<DTDCRateResponse?> GetRatesAsync(
        string apiKey,
        DTDCRateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/rate/calculate");
            httpRequest.Headers.Add("X-API-Key", apiKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("DTDC rate calculation failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DTDCRateResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC rate calculation error");
            throw;
        }
    }

    public async Task<byte[]?> GetLabelAsync(
        string apiKey,
        string consignmentNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/label/{consignmentNumber}");
            request.Headers.Add("X-API-Key", apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("DTDC label generation failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DTDC label generation error");
            throw;
        }
    }
}
