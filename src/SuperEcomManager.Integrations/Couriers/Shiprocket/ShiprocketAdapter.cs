using System.Text.Json;
using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Couriers.Shiprocket.Models;

namespace SuperEcomManager.Integrations.Couriers.Shiprocket;

/// <summary>
/// Shiprocket implementation of ICourierAdapter.
/// </summary>
public class ShiprocketAdapter : ICourierAdapter
{
    private readonly IShiprocketClient _client;
    private readonly ILogger<ShiprocketAdapter> _logger;

    public CourierType CourierType => CourierType.Shiprocket;
    public string DisplayName => "Shiprocket";

    public ShiprocketAdapter(
        IShiprocketClient client,
        ILogger<ShiprocketAdapter> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CourierResult> ValidateCredentialsAsync(
        CourierCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Shiprocket uses email/password for authentication
            var email = credentials.ApiKey; // We store email in ApiKey field
            var password = credentials.ApiSecret; // We store password in ApiSecret field

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return CourierResult.Failure("Email and password are required");
            }

            var authResponse = await _client.AuthenticateAsync(email, password, cancellationToken);

            if (authResponse == null || string.IsNullOrEmpty(authResponse.Token))
            {
                return CourierResult.Failure("Authentication failed");
            }

            return CourierResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket validation error");
            return CourierResult.Failure(ex.Message);
        }
    }

    public async Task<CourierResult<List<CourierRate>>> GetRatesAsync(
        CourierCredentials credentials,
        RateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetTokenAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult<List<CourierRate>>.Failure("Authentication failed");
            }

            var response = await _client.CheckServiceabilityAsync(
                token,
                request.PickupPincode,
                request.DeliveryPincode,
                request.Weight,
                request.IsCOD,
                cancellationToken: cancellationToken);

            if (response?.Data?.AvailableCourierCompanies == null)
            {
                return CourierResult<List<CourierRate>>.Failure("No couriers available for this route");
            }

            var rates = response.Data.AvailableCourierCompanies
                .Where(c => c.Blocked == 0)
                .Select(c => new CourierRate
                {
                    ServiceCode = c.Id.ToString(),
                    ServiceName = c.Name,
                    FreightCharge = c.FreightCharge,
                    CODCharge = c.CodCharges,
                    TotalCharge = c.Rate,
                    EstimatedDays = ParseEstimatedDays(c.EstimatedDeliveryDays),
                    ExpectedDelivery = ParseEtd(c.Etd),
                    IsExpress = c.Mode == 1,
                    IsSurface = c.IsSurface
                })
                .OrderBy(r => r.TotalCharge)
                .ToList();

            return CourierResult<List<CourierRate>>.Success(rates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket get rates error");
            return CourierResult<List<CourierRate>>.Failure(ex.Message);
        }
    }

    public async Task<CourierResult<ShipmentResponse>> CreateShipmentAsync(
        CourierCredentials credentials,
        ShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetTokenAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult<ShipmentResponse>.Failure("Authentication failed");
            }

            // Get pickup location from settings
            var pickupLocation = "Primary";
            if (credentials.AdditionalSettings.TryGetValue("pickup_location", out var pl))
            {
                pickupLocation = pl;
            }

            int? channelId = null;
            if (credentials.AdditionalSettings.TryGetValue("channel_id", out var chId) && int.TryParse(chId, out var cid))
            {
                channelId = cid;
            }

            // Create order in Shiprocket
            var orderRequest = new ShiprocketCreateOrderRequest
            {
                OrderId = request.OrderNumber,
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
                PickupLocation = pickupLocation,
                ChannelId = channelId,
                BillingCustomerName = request.DeliveryName,
                BillingAddress = request.DeliveryAddress,
                BillingCity = request.DeliveryCity,
                BillingState = request.DeliveryState,
                BillingPincode = request.DeliveryPincode,
                BillingPhone = request.DeliveryPhone,
                BillingCountry = "India",
                ShippingIsBilling = true,
                PaymentMethod = request.IsCOD ? "COD" : "Prepaid",
                SubTotal = request.DeclaredValue,
                Weight = request.Weight,
                Length = request.Length ?? 10,
                Breadth = request.Width ?? 10,
                Height = request.Height ?? 10,
                OrderItems = request.Items.Select(i => new ShiprocketOrderItem
                {
                    Name = i.Name,
                    Sku = i.Sku ?? "SKU",
                    Units = i.Quantity,
                    SellingPrice = i.UnitPrice,
                    Discount = 0,
                    Tax = 0
                }).ToList()
            };

            var orderResponse = await _client.CreateOrderAsync(token, orderRequest, cancellationToken);

            if (orderResponse == null || orderResponse.OrderId == 0)
            {
                return CourierResult<ShipmentResponse>.Failure("Failed to create order in Shiprocket");
            }

            // If shipment was auto-created and AWB assigned
            if (!string.IsNullOrEmpty(orderResponse.AwbCode))
            {
                return CourierResult<ShipmentResponse>.Success(new ShipmentResponse
                {
                    AwbNumber = orderResponse.AwbCode,
                    CourierName = orderResponse.CourierName,
                    ShipmentId = orderResponse.ShipmentId?.ToString(),
                    TrackingUrl = $"https://shiprocket.co/tracking/{orderResponse.AwbCode}"
                });
            }

            // Generate AWB if not auto-assigned
            if (orderResponse.ShipmentId.HasValue)
            {
                var awbRequest = new ShiprocketGenerateAwbRequest
                {
                    ShipmentId = orderResponse.ShipmentId.Value
                };

                // If specific courier was requested
                if (!string.IsNullOrEmpty(request.ServiceCode) && int.TryParse(request.ServiceCode, out var courierId))
                {
                    awbRequest.CourierId = courierId;
                }

                var awbResponse = await _client.GenerateAwbAsync(token, awbRequest, cancellationToken);

                if (awbResponse?.Response?.Data != null)
                {
                    var awbData = awbResponse.Response.Data;
                    return CourierResult<ShipmentResponse>.Success(new ShipmentResponse
                    {
                        AwbNumber = awbData.AwbCode,
                        CourierName = awbData.CourierName,
                        ShipmentId = awbData.ShipmentId.ToString(),
                        TrackingUrl = $"https://shiprocket.co/tracking/{awbData.AwbCode}"
                    });
                }
            }

            return CourierResult<ShipmentResponse>.Failure("Failed to generate AWB");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket create shipment error");
            return CourierResult<ShipmentResponse>.Failure(ex.Message);
        }
    }

    public async Task<CourierResult<TrackingResponse>> GetTrackingAsync(
        CourierCredentials credentials,
        string awbNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetTokenAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult<TrackingResponse>.Failure("Authentication failed");
            }

            var response = await _client.GetTrackingAsync(token, awbNumber, cancellationToken);

            if (response?.TrackingData == null)
            {
                return CourierResult<TrackingResponse>.Failure("Tracking information not found");
            }

            var trackData = response.TrackingData;
            var latestTrack = trackData.ShipmentTrack.FirstOrDefault();

            var result = new TrackingResponse
            {
                AwbNumber = awbNumber,
                CurrentStatus = latestTrack?.CurrentStatus ?? MapShipmentStatus(trackData.ShipmentStatus),
                CurrentLocation = trackData.ShipmentTrackActivities.FirstOrDefault()?.Location,
                ExpectedDelivery = ParseEtd(trackData.Etd),
                DeliveredAt = ParseDate(latestTrack?.DeliveredDate),
                DeliveredTo = latestTrack?.DeliveredTo,
                Events = trackData.ShipmentTrackActivities
                    .Select(a => new TrackingEvent
                    {
                        Timestamp = ParseDate(a.Date) ?? DateTime.UtcNow,
                        Status = a.SrStatusLabel ?? a.Status ?? "",
                        Location = a.Location,
                        Remarks = a.Activity
                    })
                    .ToList()
            };

            return CourierResult<TrackingResponse>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket tracking error");
            return CourierResult<TrackingResponse>.Failure(ex.Message);
        }
    }

    public async Task<CourierResult> CancelShipmentAsync(
        CourierCredentials credentials,
        string awbNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetTokenAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult.Failure("Authentication failed");
            }

            // Note: Shiprocket cancellation works with order IDs, not AWB numbers
            // We would need to look up the order ID from the AWB, which requires storing that mapping
            // For now, we'll log this limitation
            _logger.LogWarning("Shiprocket cancellation requires order ID mapping. AWB: {Awb}", awbNumber);

            return CourierResult.Failure("Cancellation requires order ID. Please cancel from Shiprocket dashboard.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket cancel error");
            return CourierResult.Failure(ex.Message);
        }
    }

    public async Task<CourierResult<byte[]>> GetLabelAsync(
        CourierCredentials credentials,
        string awbNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetTokenAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult<byte[]>.Failure("Authentication failed");
            }

            // Note: Shiprocket uses shipment IDs for labels, not AWB
            // We would need the shipment ID mapping
            _logger.LogWarning("Shiprocket label requires shipment ID mapping. AWB: {Awb}", awbNumber);

            return CourierResult<byte[]>.Failure("Label generation requires shipment ID");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket label error");
            return CourierResult<byte[]>.Failure(ex.Message);
        }
    }

    public async Task<CourierResult<PickupResponse>> SchedulePickupAsync(
        CourierCredentials credentials,
        PickupRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetTokenAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult<PickupResponse>.Failure("Authentication failed");
            }

            // Convert AWB numbers to shipment IDs (would need mapping)
            // For now, we'll assume the AWB numbers are actually shipment IDs
            var shipmentIds = request.AwbNumbers
                .Select(a => long.TryParse(a, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            if (shipmentIds.Count == 0)
            {
                return CourierResult<PickupResponse>.Failure("No valid shipment IDs provided");
            }

            var response = await _client.SchedulePickupAsync(token, shipmentIds, cancellationToken);

            if (response?.PickupStatus != 1)
            {
                return CourierResult<PickupResponse>.Failure("Failed to schedule pickup");
            }

            return CourierResult<PickupResponse>.Success(new PickupResponse
            {
                PickupId = response.Response?.PickupTokenNumber,
                ScheduledDate = request.PickupDate,
                ShipmentCount = shipmentIds.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket pickup scheduling error");
            return CourierResult<PickupResponse>.Failure(ex.Message);
        }
    }

    #region Private Helpers

    private async Task<string?> GetTokenAsync(CourierCredentials credentials, CancellationToken cancellationToken)
    {
        // If we have a cached token, use it
        if (!string.IsNullOrEmpty(credentials.AccessToken))
        {
            return credentials.AccessToken;
        }

        // Otherwise, authenticate
        var email = credentials.ApiKey;
        var password = credentials.ApiSecret;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var authResponse = await _client.AuthenticateAsync(email, password, cancellationToken);
        return authResponse?.Token;
    }

    private static int ParseEstimatedDays(string? etd)
    {
        if (string.IsNullOrEmpty(etd)) return 7;

        // Format: "3-5" or "5" etc.
        var parts = etd.Split('-');
        if (int.TryParse(parts[0].Trim(), out var days))
        {
            return days;
        }

        return 7;
    }

    private static DateTime? ParseEtd(string? etd)
    {
        if (string.IsNullOrEmpty(etd)) return null;

        if (DateTime.TryParse(etd, out var date))
        {
            return date;
        }

        return null;
    }

    private static DateTime? ParseDate(string? date)
    {
        if (string.IsNullOrEmpty(date)) return null;

        if (DateTime.TryParse(date, out var result))
        {
            return result;
        }

        return null;
    }

    private static string MapShipmentStatus(int status)
    {
        return status switch
        {
            1 => "Picked Up",
            2 => "In Transit",
            3 => "Out for Delivery",
            4 => "Delivered",
            5 => "RTO Initiated",
            6 => "RTO Delivered",
            7 => "Cancelled",
            8 => "Lost",
            9 => "NDR",
            _ => "Processing"
        };
    }

    #endregion
}
