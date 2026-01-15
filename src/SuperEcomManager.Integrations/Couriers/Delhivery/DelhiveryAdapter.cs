using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Couriers.Delhivery.Models;

namespace SuperEcomManager.Integrations.Couriers.Delhivery;

/// <summary>
/// Delhivery implementation of ICourierAdapter.
/// </summary>
public class DelhiveryAdapter : ICourierAdapter
{
    private readonly IDelhiveryClient _client;
    private readonly ILogger<DelhiveryAdapter> _logger;

    public CourierType CourierType => CourierType.Delhivery;
    public string DisplayName => "Delhivery";

    public DelhiveryAdapter(
        IDelhiveryClient client,
        ILogger<DelhiveryAdapter> logger)
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
            // Delhivery uses a static API token
            var token = credentials.ApiKey;

            if (string.IsNullOrEmpty(token))
            {
                return CourierResult.Failure("API token is required");
            }

            // Test credentials by checking a common pincode
            var response = await _client.CheckPincodeServiceabilityAsync(token, "110001", cancellationToken);

            if (response?.DeliveryCodes == null)
            {
                return CourierResult.Failure("Invalid API token or connection error");
            }

            return CourierResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery validation error");
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
            var token = credentials.ApiKey;
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult<List<CourierRate>>.Failure("API token is required");
            }

            // Check both pickup and delivery pincodes for serviceability
            var pickupCheck = await _client.CheckPincodeServiceabilityAsync(token, request.PickupPincode, cancellationToken);
            var deliveryCheck = await _client.CheckPincodeServiceabilityAsync(token, request.DeliveryPincode, cancellationToken);

            if (pickupCheck?.DeliveryCodes == null || pickupCheck.DeliveryCodes.Count == 0)
            {
                return CourierResult<List<CourierRate>>.Failure($"Pickup pincode {request.PickupPincode} is not serviceable");
            }

            if (deliveryCheck?.DeliveryCodes == null || deliveryCheck.DeliveryCodes.Count == 0)
            {
                return CourierResult<List<CourierRate>>.Failure($"Delivery pincode {request.DeliveryPincode} is not serviceable");
            }

            var deliveryInfo = deliveryCheck.DeliveryCodes.FirstOrDefault()?.PostalCode;

            // Check if COD is supported if requested
            if (request.IsCOD && deliveryInfo?.Cod?.ToLower() != "y")
            {
                return CourierResult<List<CourierRate>>.Failure("COD is not available for this route");
            }

            // Delhivery provides Express and Surface modes
            var rates = new List<CourierRate>();

            // Express service
            rates.Add(new CourierRate
            {
                ServiceCode = "E",
                ServiceName = "Delhivery Express",
                FreightCharge = CalculateEstimatedFreight(request.Weight, false),
                CODCharge = request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0,
                TotalCharge = CalculateEstimatedFreight(request.Weight, false) +
                              (request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0),
                EstimatedDays = 3,
                ExpectedDelivery = DateTime.UtcNow.AddDays(3),
                IsExpress = true,
                IsSurface = false
            });

            // Surface service (only for non-express)
            rates.Add(new CourierRate
            {
                ServiceCode = "S",
                ServiceName = "Delhivery Surface",
                FreightCharge = CalculateEstimatedFreight(request.Weight, true),
                CODCharge = request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0,
                TotalCharge = CalculateEstimatedFreight(request.Weight, true) +
                              (request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0),
                EstimatedDays = 7,
                ExpectedDelivery = DateTime.UtcNow.AddDays(7),
                IsExpress = false,
                IsSurface = true
            });

            return CourierResult<List<CourierRate>>.Success(rates.OrderBy(r => r.TotalCharge).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery get rates error");
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
            var token = credentials.ApiKey;
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult<ShipmentResponse>.Failure("API token is required");
            }

            // Get pickup location from settings
            var pickupLocation = "Primary";
            if (credentials.AdditionalSettings.TryGetValue("pickup_location", out var pl))
            {
                pickupLocation = pl;
            }

            // Generate waybill first if not using auto-generated
            string? waybill = null;
            var waybillResponse = await _client.GenerateWaybillsAsync(token, 1, cancellationToken);
            if (waybillResponse?.Waybills?.Count > 0)
            {
                waybill = waybillResponse.Waybills[0];
            }

            var createRequest = new DelhiveryCreateShipmentRequest
            {
                Shipments = new List<DelhiveryShipment>
                {
                    new DelhiveryShipment
                    {
                        Name = request.DeliveryName,
                        Address = request.DeliveryAddress,
                        Pincode = request.DeliveryPincode,
                        City = request.DeliveryCity,
                        State = request.DeliveryState,
                        Country = "India",
                        Phone = request.DeliveryPhone,
                        OrderId = request.OrderNumber,
                        PaymentMode = request.IsCOD ? "COD" : "Prepaid",
                        CodAmount = request.IsCOD ? (request.CODAmount ?? request.DeclaredValue) : 0,
                        TotalAmount = request.DeclaredValue,
                        OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        ProductsDescription = string.Join(", ", request.Items.Select(i => i.Name)),
                        Quantity = request.Items.Sum(i => i.Quantity),
                        Waybill = waybill,
                        Weight = request.Weight * 1000, // Convert kg to grams
                        Length = request.Length,
                        Width = request.Width,
                        Height = request.Height,
                        ReturnPincode = request.PickupPincode,
                        ReturnCity = request.PickupCity,
                        ReturnState = request.PickupState,
                        ReturnAddress = request.PickupAddress,
                        ReturnPhone = request.PickupPhone,
                        ReturnName = request.PickupName,
                        ReturnCountry = "India"
                    }
                },
                PickupLocation = new DelhiveryPickupLocation
                {
                    Name = pickupLocation,
                    Address = request.PickupAddress,
                    City = request.PickupCity,
                    Pincode = request.PickupPincode,
                    Phone = request.PickupPhone
                }
            };

            var response = await _client.CreateShipmentAsync(token, createRequest, cancellationToken);

            if (response == null || !response.Success)
            {
                return CourierResult<ShipmentResponse>.Failure(
                    response?.Remarks ?? "Failed to create shipment in Delhivery");
            }

            var package = response.Packages?.FirstOrDefault();
            if (package == null || string.IsNullOrEmpty(package.Waybill))
            {
                return CourierResult<ShipmentResponse>.Failure("No waybill received from Delhivery");
            }

            return CourierResult<ShipmentResponse>.Success(new ShipmentResponse
            {
                AwbNumber = package.Waybill,
                CourierName = "Delhivery",
                ShipmentId = package.ReferenceNumber,
                TrackingUrl = $"https://www.delhivery.com/track/package/{package.Waybill}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery create shipment error");
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
            var token = credentials.ApiKey;
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult<TrackingResponse>.Failure("API token is required");
            }

            var response = await _client.GetTrackingAsync(token, awbNumber, cancellationToken);

            if (response?.ShipmentData == null || response.ShipmentData.Count == 0)
            {
                return CourierResult<TrackingResponse>.Failure("Tracking information not found");
            }

            var shipment = response.ShipmentData[0].Shipment;
            if (shipment == null)
            {
                return CourierResult<TrackingResponse>.Failure("Invalid tracking response");
            }

            var result = new TrackingResponse
            {
                AwbNumber = awbNumber,
                CurrentStatus = shipment.Status?.Status ?? "Unknown",
                CurrentLocation = shipment.Status?.StatusLocation,
                ExpectedDelivery = ParseDate(shipment.PromisedDeliveryDate ?? shipment.ExpectedDeliveryDate),
                DeliveredAt = shipment.Status?.StatusType?.ToLower() == "dl" ?
                    ParseDate(shipment.Status.StatusDateTime) : null,
                DeliveredTo = shipment.Status?.ReceivedBy,
                Events = (shipment.Scans ?? new List<DelhiveryScan>())
                    .Where(s => s.ScanDetail != null)
                    .Select(s => new TrackingEvent
                    {
                        Timestamp = ParseDate(s.ScanDetail!.ScanDateTime) ?? DateTime.UtcNow,
                        Status = s.ScanDetail.Scan ?? "",
                        Location = s.ScanDetail.ScannedLocation,
                        Remarks = s.ScanDetail.Instructions
                    })
                    .ToList()
            };

            return CourierResult<TrackingResponse>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery tracking error");
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
            var token = credentials.ApiKey;
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult.Failure("API token is required");
            }

            var response = await _client.CancelShipmentAsync(token, awbNumber, cancellationToken);

            if (response == null || !response.Status)
            {
                return CourierResult.Failure(response?.Remarks ?? "Failed to cancel shipment");
            }

            return CourierResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery cancel error");
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
            var token = credentials.ApiKey;
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult<byte[]>.Failure("API token is required");
            }

            var labelData = await _client.GetLabelAsync(token, awbNumber, cancellationToken);

            if (labelData == null || labelData.Length == 0)
            {
                return CourierResult<byte[]>.Failure("Failed to generate label");
            }

            return CourierResult<byte[]>.Success(labelData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery label error");
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
            var token = credentials.ApiKey;
            if (string.IsNullOrEmpty(token))
            {
                return CourierResult<PickupResponse>.Failure("API token is required");
            }

            // Get pickup location from settings
            var pickupLocation = "Primary";
            if (credentials.AdditionalSettings.TryGetValue("pickup_location", out var pl))
            {
                pickupLocation = pl;
            }

            var pickupRequest = new DelhiveryPickupRequest
            {
                PickupLocation = pickupLocation,
                PickupDate = request.PickupDate.ToString("yyyy-MM-dd"),
                PickupTime = request.PickupTimeSlot ?? "10:00 - 18:00",
                ExpectedPackageCount = request.AwbNumbers.Count
            };

            var response = await _client.SchedulePickupAsync(token, pickupRequest, cancellationToken);

            if (response == null || !response.Success)
            {
                return CourierResult<PickupResponse>.Failure(
                    response?.Message ?? "Failed to schedule pickup");
            }

            return CourierResult<PickupResponse>.Success(new PickupResponse
            {
                PickupId = response.PickupId,
                ScheduledDate = request.PickupDate,
                TimeSlot = response.PickupTime,
                ShipmentCount = request.AwbNumbers.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delhivery pickup scheduling error");
            return CourierResult<PickupResponse>.Failure(ex.Message);
        }
    }

    #region Private Helpers

    private static decimal CalculateEstimatedFreight(decimal weightKg, bool isSurface)
    {
        // Basic freight calculation (actual rates depend on contract)
        // These are approximate base rates for estimation
        var baseRate = isSurface ? 35m : 50m;
        var perKgRate = isSurface ? 15m : 25m;

        var chargeableWeight = Math.Max(0.5m, Math.Ceiling(weightKg * 2) / 2); // Round to 0.5 kg

        return baseRate + (chargeableWeight * perKgRate);
    }

    private static decimal CalculateCodCharge(decimal codAmount)
    {
        // Typical COD charge: 2% of COD amount or minimum Rs. 50
        var codCharge = codAmount * 0.02m;
        return Math.Max(50m, codCharge);
    }

    private static DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;

        if (DateTime.TryParse(dateString, out var date))
        {
            return date;
        }

        return null;
    }

    #endregion
}
