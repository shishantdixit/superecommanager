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
            // Shiprocket uses email/password for authentication.
            // These must be API user credentials (Settings → API → API Users),
            // not regular account credentials which trigger OTP authentication.
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
            _logger.LogInformation("=== SHIPROCKET CREATE SHIPMENT STARTED ===");
            _logger.LogInformation("Order Number: {OrderNumber}, COD: {IsCOD}, Weight: {Weight}kg",
                request.OrderNumber, request.IsCOD, request.Weight);

            var token = await GetTokenAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Shiprocket authentication failed - no token received");
                return CourierResult<ShipmentResponse>.Failure("Authentication failed");
            }

            _logger.LogInformation("Shiprocket authentication successful, token received");

            // Get pickup location from settings
            var pickupLocation = "Primary";
            if (credentials.AdditionalSettings.TryGetValue("pickupLocation", out var pl))
            {
                pickupLocation = pl;
            }

            // Resolve channel ID: primary from credentials, fallback from additional settings
            int? channelId = null;
            if (!string.IsNullOrEmpty(credentials.ChannelId) && int.TryParse(credentials.ChannelId, out var credChId))
            {
                channelId = credChId;
            }
            else if (credentials.AdditionalSettings.TryGetValue("channelId", out var chId) && int.TryParse(chId, out var cid))
            {
                channelId = cid;
            }

            _logger.LogInformation("Pickup Location: {PickupLocation}, Channel ID: {ChannelId}",
                pickupLocation, channelId?.ToString() ?? "None");

            // Split customer name into first and last name for Shiprocket validation
            var nameParts = request.DeliveryName.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            // Create order in Shiprocket
            var orderRequest = new ShiprocketCreateOrderRequest
            {
                OrderId = request.OrderNumber,
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"),
                PickupLocation = pickupLocation,
                ChannelId = channelId,
                BillingCustomerName = firstName,
                BillingLastName = lastName,
                BillingAddress = request.DeliveryAddress,
                BillingCity = request.DeliveryCity,
                BillingState = request.DeliveryState,
                BillingPincode = request.DeliveryPincode,
                BillingPhone = request.DeliveryPhone,
                BillingCountry = "India",
                ShippingIsBilling = 1, // 1 = billing address same as shipping
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
                    Tax = 0,
                    Hsn = "0" // Default HSN code for general goods (required by Shiprocket)
                }).ToList()
            };

            // Use channel-specific endpoint if channel_id is set, otherwise adhoc
            ShiprocketCreateOrderResponse? orderResponse;
            if (channelId.HasValue)
            {
                _logger.LogInformation("Calling Shiprocket CreateOrder API (channel-specific, channel_id={ChannelId})...", channelId.Value);
                orderResponse = await _client.CreateChannelOrderAsync(token, orderRequest, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Calling Shiprocket CreateOrder API (adhoc/custom)...");
                orderResponse = await _client.CreateOrderAsync(token, orderRequest, cancellationToken);
            }

            if (orderResponse == null || orderResponse.OrderId == 0)
            {
                // Build comprehensive error message
                string errorMessage = "Failed to create order in Shiprocket";

                if (orderResponse != null)
                {
                    // Check for structured error response
                    if (!string.IsNullOrEmpty(orderResponse.Message))
                    {
                        errorMessage = orderResponse.Message;

                        // Append detailed validation errors if present
                        if (orderResponse.Errors != null && orderResponse.Errors.Any())
                        {
                            var errorDetails = string.Join("; ", orderResponse.Errors
                                .SelectMany(e => e.Value.Select(v => $"{e.Key}: {v}")));
                            errorMessage += $" - {errorDetails}";
                        }
                    }
                    // Fallback to Status field
                    else if (!string.IsNullOrEmpty(orderResponse.Status))
                    {
                        errorMessage = orderResponse.Status;
                    }

                    _logger.LogError("Shiprocket CreateOrder API failed. StatusCode: {StatusCode}, Message: {Message}, Errors: {@Errors}",
                        orderResponse.StatusCode, orderResponse.Message, orderResponse.Errors);
                }
                else
                {
                    _logger.LogError("Shiprocket CreateOrder API returned null response");
                }

                return CourierResult<ShipmentResponse>.Failure(errorMessage);
            }

            _logger.LogInformation("Shiprocket order created successfully. OrderId: {OrderId}, ShipmentId: {ShipmentId}, AWB: {AwbCode}",
                orderResponse.OrderId, orderResponse.ShipmentId, orderResponse.AwbCode ?? "Not assigned yet");

            // If shipment was auto-created and AWB assigned
            if (!string.IsNullOrEmpty(orderResponse.AwbCode))
            {
                _logger.LogInformation("AWB auto-assigned: {AwbCode}, Courier: {CourierName}",
                    orderResponse.AwbCode, orderResponse.CourierName);
                return CourierResult<ShipmentResponse>.Success(new ShipmentResponse
                {
                    AwbNumber = orderResponse.AwbCode,
                    CourierName = orderResponse.CourierName,
                    ShipmentId = orderResponse.ShipmentId?.ToString(),
                    TrackingUrl = $"https://shiprocket.co/tracking/{orderResponse.AwbCode}",
                    ExternalOrderId = orderResponse.OrderId.ToString(),
                    ExternalShipmentId = orderResponse.ShipmentId?.ToString()
                });
            }

            // Generate AWB if not auto-assigned
            if (orderResponse.ShipmentId.HasValue)
            {
                _logger.LogInformation("AWB not auto-assigned, calling GenerateAwb API for ShipmentId: {ShipmentId}...",
                    orderResponse.ShipmentId.Value);

                var awbRequest = new ShiprocketGenerateAwbRequest
                {
                    ShipmentId = orderResponse.ShipmentId.Value
                };

                // If specific courier was requested
                if (!string.IsNullOrEmpty(request.ServiceCode) && int.TryParse(request.ServiceCode, out var courierId))
                {
                    awbRequest.CourierId = courierId;
                    _logger.LogInformation("Using specific courier ID: {CourierId}", courierId);
                }
                else
                {
                    _logger.LogInformation("No courier ID specified - Shiprocket will auto-select based on serviceability");
                }

                _logger.LogInformation("Calling Shiprocket AWB generation API with request: {@AwbRequest}", awbRequest);

                var awbResponse = await _client.GenerateAwbAsync(token, awbRequest, cancellationToken);

                _logger.LogInformation("AWB generation API response: {@AwbResponse}", awbResponse);

                if (awbResponse?.Response?.Data != null)
                {
                    var awbData = awbResponse.Response.Data;
                    _logger.LogInformation("AWB generated successfully: {AwbCode}, Courier: {CourierName}, CourierId: {CourierId}",
                        awbData.AwbCode, awbData.CourierName, awbData.CourierCompanyId);

                    return CourierResult<ShipmentResponse>.Success(new ShipmentResponse
                    {
                        AwbNumber = awbData.AwbCode,
                        CourierName = awbData.CourierName,
                        ShipmentId = awbData.ShipmentId.ToString(),
                        TrackingUrl = $"https://shiprocket.co/tracking/{awbData.AwbCode}",
                        LabelUrl = null, // Label must be generated separately via /courier/generate/label
                        ExternalOrderId = orderResponse.OrderId.ToString(),
                        ExternalShipmentId = orderResponse.ShipmentId?.ToString()
                    });
                }
                else
                {
                    string errorMessage = "Failed to assign courier";

                    if (awbResponse != null)
                    {
                        errorMessage += $". AWB Assign Status: {awbResponse.AwbAssignStatus}";

                        if (awbResponse.AwbAssignStatus == 0)
                        {
                            errorMessage += ". Possible reasons: No serviceable courier found for this route, or insufficient wallet balance.";
                        }
                    }

                    _logger.LogWarning("AWB assignment failed but order was created. Returning partial success. Error: {ErrorMessage}. Full Response: {@Response}",
                        errorMessage, awbResponse);

                    // Return partial success - order was created, AWB assignment failed
                    // This allows the shipment to be saved and courier assigned later
                    return CourierResult<ShipmentResponse>.Success(new ShipmentResponse
                    {
                        AwbNumber = string.Empty,
                        ExternalOrderId = orderResponse.OrderId.ToString(),
                        ExternalShipmentId = orderResponse.ShipmentId?.ToString(),
                        IsPartialSuccess = true,
                        AwbError = errorMessage
                    });
                }
            }
            else
            {
                // Order was created but no shipment ID returned - rare edge case
                _logger.LogWarning("No shipment ID in order response - returning partial success with order ID only. OrderResponse: {@OrderResponse}",
                    orderResponse);

                return CourierResult<ShipmentResponse>.Success(new ShipmentResponse
                {
                    AwbNumber = string.Empty,
                    ExternalOrderId = orderResponse.OrderId.ToString(),
                    ExternalShipmentId = null,
                    IsPartialSuccess = true,
                    AwbError = "Order created but no shipment ID was returned. Please check the order in Shiprocket dashboard."
                });
            }
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
