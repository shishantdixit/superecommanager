using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperEcomManager.Integrations.Couriers.BlueDart.Models;

namespace SuperEcomManager.Integrations.Couriers.BlueDart;

/// <summary>
/// HTTP client implementation for BlueDart API.
/// BlueDart uses SOAP-based API but we'll use their REST/JSON endpoints where available.
/// </summary>
public class BlueDartClient : IBlueDartClient
{
    private readonly HttpClient _httpClient;
    private readonly BlueDartSettings _settings;
    private readonly ILogger<BlueDartClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null // BlueDart uses PascalCase
    };

    public BlueDartClient(
        HttpClient httpClient,
        IOptions<BlueDartSettings> settings,
        ILogger<BlueDartClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<BlueDartWaybillResponse?> GenerateWaybillAsync(
        BlueDartProfile profile,
        BlueDartWaybillRequestData request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                profile.LoginId,
                profile.LicenseKey,
                profile.ApiType,
                Request = request
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/Ver1.10/ShippingAPI/WayBill/WayBillGeneration.svc/rest/GenerateWayBill");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("BlueDart waybill generation failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BlueDartWaybillResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart waybill generation error");
            throw;
        }
    }

    public async Task<BlueDartPincodeResponse?> CheckPincodeServiceabilityAsync(
        BlueDartProfile profile,
        string pincode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                profile.LoginId,
                profile.LicenseKey,
                profile.ApiType,
                pinCode = pincode
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/Ver1.10/ShippingAPI/Finder/ServiceFinderQuery.svc/rest/GetServicesforPincode");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("BlueDart pincode check failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BlueDartPincodeResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart pincode check error");
            throw;
        }
    }

    public async Task<BlueDartTrackingResponse?> GetTrackingAsync(
        BlueDartProfile profile,
        string awbNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                profile.LoginId,
                profile.LicenseKey,
                profile.ApiType,
                AWBNo = awbNumber
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/Ver1.10/ShippingAPI/Tracking/TrackingQuery.svc/rest/GetShipmentTracking");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("BlueDart tracking failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BlueDartTrackingResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart tracking error");
            throw;
        }
    }

    public async Task<BlueDartPickupResponse?> SchedulePickupAsync(
        BlueDartProfile profile,
        BlueDartPickupRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                profile.LoginId,
                profile.LicenseKey,
                profile.ApiType,
                Request = request
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/Ver1.10/ShippingAPI/Pickup/PickupRegistration.svc/rest/RegisterPickup");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("BlueDart pickup scheduling failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BlueDartPickupResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart pickup scheduling error");
            throw;
        }
    }

    public async Task<BlueDartCancelResponse?> CancelWaybillAsync(
        BlueDartProfile profile,
        string awbNumber,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                profile.LoginId,
                profile.LicenseKey,
                profile.ApiType,
                AWBNo = awbNumber,
                CancellationReason = reason
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/Ver1.10/ShippingAPI/WayBill/WayBillGeneration.svc/rest/CancelWaybill");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("BlueDart cancellation failed: {Error}", error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<BlueDartCancelResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart cancellation error");
            throw;
        }
    }

    public async Task<byte[]?> GetLabelAsync(
        BlueDartProfile profile,
        string awbNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                profile.LoginId,
                profile.LicenseKey,
                profile.ApiType,
                AWBNo = awbNumber,
                PrintFormat = "PDF"
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/Ver1.10/ShippingAPI/Manifest/Manifest.svc/rest/PrintAWB");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("BlueDart label generation failed: {Error}", error);
                return null;
            }

            // The response contains base64 encoded PDF
            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(result);

            if (jsonDoc.RootElement.TryGetProperty("PrintAWBResult", out var printResult))
            {
                if (printResult.TryGetProperty("AWBPrintContent", out var content))
                {
                    var base64 = content.GetString();
                    if (!string.IsNullOrEmpty(base64))
                    {
                        return Convert.FromBase64String(base64);
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart label generation error");
            throw;
        }
    }
}
