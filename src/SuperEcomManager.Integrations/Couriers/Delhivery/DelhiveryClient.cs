using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperEcomManager.Integrations.Couriers.Delhivery.Models;

namespace SuperEcomManager.Integrations.Couriers.Delhivery;

/// <summary>
/// HTTP client implementation for Delhivery API.
/// </summary>
public class DelhiveryClient : IDelhiveryClient
{
    private readonly HttpClient _httpClient;
    private readonly DelhiverySettings _settings;
    private readonly ILogger<DelhiveryClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public DelhiveryClient(
        HttpClient httpClient,
        IOptions<DelhiverySettings> settings,
        ILogger<DelhiveryClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<DelhiveryCreateShipmentResponse?> CreateShipmentAsync(
        string token,
        DelhiveryCreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Delhivery expects the shipment data as form-urlencoded with a 'data' parameter
            var shipmentJson = JsonSerializer.Serialize(request, JsonOptions);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/cmu/create.json");
            httpRequest.Headers.Add("Authorization", $"Token {token}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("format", "json"),
                new KeyValuePair<string, string>("data", shipmentJson)
            });
            httpRequest.Content = content;

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Delhivery create shipment failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DelhiveryCreateShipmentResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery create shipment error");
            throw;
        }
    }

    public async Task<DelhiveryWaybillResponse?> GenerateWaybillsAsync(
        string token,
        int count = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/waybill/api/fetch/json/?count={count}");
            request.Headers.Add("Authorization", $"Token {token}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Delhivery waybill generation failed: {Error}", error);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Delhivery returns waybills as a plain string list
            var waybills = content.Split(',')
                .Select(w => w.Trim().Trim('"', '[', ']'))
                .Where(w => !string.IsNullOrEmpty(w))
                .ToList();

            return new DelhiveryWaybillResponse
            {
                Success = waybills.Count > 0,
                Waybills = waybills
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery waybill generation error");
            throw;
        }
    }

    public async Task<DelhiveryPincodeResponse?> CheckPincodeServiceabilityAsync(
        string token,
        string pincode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/c/api/pin-codes/json/?filter_codes={pincode}");
            request.Headers.Add("Authorization", $"Token {token}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Delhivery pincode check failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DelhiveryPincodeResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery pincode check error");
            throw;
        }
    }

    public async Task<DelhiveryTrackingResponse?> GetTrackingAsync(
        string token,
        string waybill,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use tracking URL which might be different from base URL
            var trackingUrl = $"{_settings.TrackingUrl}/api/v1/packages/json/?waybill={waybill}";

            using var request = new HttpRequestMessage(HttpMethod.Get, trackingUrl);
            request.Headers.Add("Authorization", $"Token {token}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Delhivery tracking failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DelhiveryTrackingResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery tracking error");
            throw;
        }
    }

    public async Task<DelhiveryTrackingResponse?> GetBulkTrackingAsync(
        string token,
        List<string> waybills,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var waybillParam = string.Join(",", waybills);
            var trackingUrl = $"{_settings.TrackingUrl}/api/v1/packages/json/?waybill={waybillParam}";

            using var request = new HttpRequestMessage(HttpMethod.Get, trackingUrl);
            request.Headers.Add("Authorization", $"Token {token}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Delhivery bulk tracking failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DelhiveryTrackingResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery bulk tracking error");
            throw;
        }
    }

    public async Task<DelhiveryPickupResponse?> SchedulePickupAsync(
        string token,
        DelhiveryPickupRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/fm/request/new/");
            httpRequest.Headers.Add("Authorization", $"Token {token}");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Delhivery pickup scheduling failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DelhiveryPickupResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery pickup scheduling error");
            throw;
        }
    }

    public async Task<DelhiveryCancelResponse?> CancelShipmentAsync(
        string token,
        string waybill,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/p/edit");
            httpRequest.Headers.Add("Authorization", $"Token {token}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("waybill", waybill),
                new KeyValuePair<string, string>("cancellation", "true")
            });
            httpRequest.Content = content;

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Delhivery cancellation failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DelhiveryCancelResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery cancellation error");
            throw;
        }
    }

    public async Task<byte[]?> GetLabelAsync(
        string token,
        string waybill,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/p/packing_slip?wbns={waybill}&pdf=true");
            request.Headers.Add("Authorization", $"Token {token}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Delhivery label generation failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery label generation error");
            throw;
        }
    }
}
