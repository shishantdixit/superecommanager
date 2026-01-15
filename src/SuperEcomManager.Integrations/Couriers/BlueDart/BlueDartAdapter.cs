using Microsoft.Extensions.Logging;
using SuperEcomManager.Domain.Enums;
using SuperEcomManager.Integrations.Couriers.BlueDart.Models;

namespace SuperEcomManager.Integrations.Couriers.BlueDart;

/// <summary>
/// BlueDart implementation of ICourierAdapter.
/// </summary>
public class BlueDartAdapter : ICourierAdapter
{
    private readonly IBlueDartClient _client;
    private readonly ILogger<BlueDartAdapter> _logger;

    public CourierType CourierType => CourierType.BlueDart;
    public string DisplayName => "BlueDart";

    public BlueDartAdapter(
        IBlueDartClient client,
        ILogger<BlueDartAdapter> logger)
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
            var profile = CreateProfile(credentials);

            if (string.IsNullOrEmpty(profile.LoginId) || string.IsNullOrEmpty(profile.LicenseKey))
            {
                return CourierResult.Failure("Login ID and License Key are required");
            }

            // Test credentials by checking a common pincode
            var response = await _client.CheckPincodeServiceabilityAsync(profile, "110001", cancellationToken);

            if (response?.Result == null || response.Result.IsError)
            {
                var errorMsg = response?.Result?.ErrorMessages?.FirstOrDefault() ?? "Invalid credentials";
                return CourierResult.Failure(errorMsg);
            }

            return CourierResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart validation error");
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
            var profile = CreateProfile(credentials);

            // Check delivery pincode serviceability
            var pincodeResponse = await _client.CheckPincodeServiceabilityAsync(
                profile, request.DeliveryPincode, cancellationToken);

            if (pincodeResponse?.Result == null || pincodeResponse.Result.IsError)
            {
                return CourierResult<List<CourierRate>>.Failure(
                    $"Pincode {request.DeliveryPincode} is not serviceable");
            }

            var pincodeResult = pincodeResponse.Result;
            var rates = new List<CourierRate>();

            // Check available services and add rates
            var availableServices = pincodeResult.AvailableServiceCodes ?? new List<string>();

            // Air Express (Product Code A)
            if (availableServices.Contains("A") || availableServices.Count == 0)
            {
                rates.Add(new CourierRate
                {
                    ServiceCode = "A",
                    ServiceName = "BlueDart Air Express",
                    FreightCharge = CalculateEstimatedFreight(request.Weight, false),
                    CODCharge = request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0,
                    TotalCharge = CalculateEstimatedFreight(request.Weight, false) +
                                  (request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0),
                    EstimatedDays = 2,
                    ExpectedDelivery = DateTime.UtcNow.AddDays(2),
                    IsExpress = true,
                    IsSurface = false
                });
            }

            // Apex (Ground/Surface - Product Code D)
            if (availableServices.Contains("D") || pincodeResult.ApexInbound == "Y" || availableServices.Count == 0)
            {
                rates.Add(new CourierRate
                {
                    ServiceCode = "D",
                    ServiceName = "BlueDart Apex (Surface)",
                    FreightCharge = CalculateEstimatedFreight(request.Weight, true),
                    CODCharge = request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0,
                    TotalCharge = CalculateEstimatedFreight(request.Weight, true) +
                                  (request.IsCOD ? CalculateCodCharge(request.CODAmount ?? 0) : 0),
                    EstimatedDays = 5,
                    ExpectedDelivery = DateTime.UtcNow.AddDays(5),
                    IsExpress = false,
                    IsSurface = true
                });
            }

            if (rates.Count == 0)
            {
                return CourierResult<List<CourierRate>>.Failure("No services available for this route");
            }

            return CourierResult<List<CourierRate>>.Success(rates.OrderBy(r => r.TotalCharge).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart get rates error");
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
            var profile = CreateProfile(credentials);

            // Get customer code from additional settings
            var customerCode = credentials.AdditionalSettings.GetValueOrDefault("customer_code", "");
            var originArea = credentials.AdditionalSettings.GetValueOrDefault("origin_area", "");

            var waybillRequest = new BlueDartWaybillRequestData
            {
                Consignee = new BlueDartConsignee
                {
                    ConsigneeName = request.DeliveryName,
                    Address1 = request.DeliveryAddress,
                    Pincode = request.DeliveryPincode,
                    Mobile = request.DeliveryPhone
                },
                Shipper = new BlueDartShipper
                {
                    Name = request.PickupName,
                    Address1 = request.PickupAddress,
                    Pincode = request.PickupPincode,
                    Mobile = request.PickupPhone,
                    CustomerCode = customerCode,
                    OriginArea = originArea,
                    IsToPayCustomer = false
                },
                Services = new BlueDartServices
                {
                    ActualWeight = request.Weight,
                    CollectableAmount = request.IsCOD ? (request.CODAmount ?? request.DeclaredValue) : 0,
                    DeclaredValue = request.DeclaredValue,
                    ItemCount = request.Items.Sum(i => i.Quantity),
                    PieceCount = 1,
                    PickupDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    PickupTime = "1000",
                    ProductCode = request.IsExpress ? "A" : (request.ServiceCode ?? "A"),
                    ProductType = 2, // Non-Docs
                    SubProductCode = "P",
                    Commodity = new BlueDartCommodity
                    {
                        Detail1 = string.Join(", ", request.Items.Select(i => i.Name).Take(30))
                    },
                    CreditReferenceNo = request.OrderNumber,
                    InvoiceNo = request.OrderNumber
                }
            };

            // Add dimensions if provided
            if (request.Length.HasValue && request.Width.HasValue && request.Height.HasValue)
            {
                waybillRequest.Services.Dimensions = new BlueDartDimensions
                {
                    DimensionList = new List<BlueDartDimension>
                    {
                        new BlueDartDimension
                        {
                            Length = request.Length.Value,
                            Breadth = request.Width.Value,
                            Height = request.Height.Value,
                            Count = 1
                        }
                    }
                };
            }

            var response = await _client.GenerateWaybillAsync(profile, waybillRequest, cancellationToken);

            if (response?.Result == null || response.Result.IsError)
            {
                var errorMsg = response?.Result?.ErrorMessages?.FirstOrDefault() ?? "Failed to create shipment";
                return CourierResult<ShipmentResponse>.Failure(errorMsg);
            }

            if (string.IsNullOrEmpty(response.Result.AwbNo))
            {
                return CourierResult<ShipmentResponse>.Failure("No AWB number received");
            }

            return CourierResult<ShipmentResponse>.Success(new ShipmentResponse
            {
                AwbNumber = response.Result.AwbNo,
                CourierName = "BlueDart",
                ShipmentId = response.Result.AwbNo,
                TrackingUrl = $"https://www.bluedart.com/tracking/{response.Result.AwbNo}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart create shipment error");
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
            var profile = CreateProfile(credentials);

            var response = await _client.GetTrackingAsync(profile, awbNumber, cancellationToken);

            if (response?.Result == null || response.Result.IsError)
            {
                var errorMsg = response?.Result?.ErrorMessages?.FirstOrDefault() ?? "Tracking not found";
                return CourierResult<TrackingResponse>.Failure(errorMsg);
            }

            var trackingDetails = response.Result.TrackingDetails;
            if (trackingDetails == null || trackingDetails.Count == 0)
            {
                return CourierResult<TrackingResponse>.Failure("No tracking details available");
            }

            var latestStatus = trackingDetails[0];

            var result = new TrackingResponse
            {
                AwbNumber = awbNumber,
                CurrentStatus = latestStatus.Status ?? "Unknown",
                CurrentLocation = latestStatus.StatusLocation,
                ExpectedDelivery = ParseDate(latestStatus.ExpectedDeliveryDate),
                DeliveredAt = latestStatus.StatusType?.ToUpper() == "DL" ?
                    ParseDateTime(latestStatus.StatusDate, latestStatus.StatusTime) : null,
                DeliveredTo = latestStatus.ReceivedBy,
                Events = trackingDetails.Select(t => new TrackingEvent
                {
                    Timestamp = ParseDateTime(t.StatusDate, t.StatusTime) ?? DateTime.UtcNow,
                    Status = t.Status ?? "",
                    Location = t.StatusLocation,
                    Remarks = t.Instructions ?? t.Remarks
                }).ToList()
            };

            return CourierResult<TrackingResponse>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart tracking error");
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
            var profile = CreateProfile(credentials);

            var response = await _client.CancelWaybillAsync(
                profile, awbNumber, "Customer Request", cancellationToken);

            if (response?.Result == null || response.Result.IsError)
            {
                var errorMsg = response?.Result?.ErrorMessages?.FirstOrDefault() ?? "Cancellation failed";
                return CourierResult.Failure(errorMsg);
            }

            return CourierResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart cancel error");
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
            var profile = CreateProfile(credentials);

            var labelData = await _client.GetLabelAsync(profile, awbNumber, cancellationToken);

            if (labelData == null || labelData.Length == 0)
            {
                return CourierResult<byte[]>.Failure("Failed to generate label");
            }

            return CourierResult<byte[]>.Success(labelData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart label error");
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
            var profile = CreateProfile(credentials);

            var customerCode = credentials.AdditionalSettings.GetValueOrDefault("customer_code", "");
            var areaCode = credentials.AdditionalSettings.GetValueOrDefault("area_code", "");
            var pickupName = credentials.AdditionalSettings.GetValueOrDefault("pickup_name", "");
            var pickupAddress = credentials.AdditionalSettings.GetValueOrDefault("pickup_address", "");
            var pickupPincode = credentials.AdditionalSettings.GetValueOrDefault("pickup_pincode", "");
            var pickupMobile = credentials.AdditionalSettings.GetValueOrDefault("pickup_mobile", "");

            var pickupRequest = new BlueDartPickupRequest
            {
                AreaCode = areaCode,
                CustomerCode = customerCode,
                CustomerName = pickupName,
                Address1 = pickupAddress,
                Pincode = pickupPincode,
                Mobile = pickupMobile,
                PickupDate = request.PickupDate.ToString("dd-MMM-yyyy"),
                NumberOfPieces = request.AwbNumbers.Count,
                ActualWeight = 0.5m * request.AwbNumbers.Count // Estimate
            };

            var response = await _client.SchedulePickupAsync(profile, pickupRequest, cancellationToken);

            if (response?.Result == null || response.Result.IsError)
            {
                var errorMsg = response?.Result?.ErrorMessages?.FirstOrDefault() ?? "Pickup scheduling failed";
                return CourierResult<PickupResponse>.Failure(errorMsg);
            }

            return CourierResult<PickupResponse>.Success(new PickupResponse
            {
                PickupId = response.Result.PickupRegistrationNumber ?? response.Result.TokenNumber,
                ScheduledDate = request.PickupDate,
                ShipmentCount = request.AwbNumbers.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlueDart pickup scheduling error");
            return CourierResult<PickupResponse>.Failure(ex.Message);
        }
    }

    #region Private Helpers

    private static BlueDartProfile CreateProfile(CourierCredentials credentials)
    {
        return new BlueDartProfile
        {
            LoginId = credentials.ApiKey ?? "",
            LicenseKey = credentials.ApiSecret ?? "",
            ApiType = "S"
        };
    }

    private static decimal CalculateEstimatedFreight(decimal weightKg, bool isSurface)
    {
        // Basic freight calculation (actual rates depend on contract)
        var baseRate = isSurface ? 45m : 65m;
        var perKgRate = isSurface ? 20m : 35m;

        var chargeableWeight = Math.Max(0.5m, Math.Ceiling(weightKg * 2) / 2);

        return baseRate + (chargeableWeight * perKgRate);
    }

    private static decimal CalculateCodCharge(decimal codAmount)
    {
        // BlueDart typically charges 2.5% of COD amount or minimum Rs. 60
        var codCharge = codAmount * 0.025m;
        return Math.Max(60m, codCharge);
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

    private static DateTime? ParseDateTime(string? date, string? time)
    {
        if (string.IsNullOrEmpty(date)) return null;

        var dateTimeStr = string.IsNullOrEmpty(time) ? date : $"{date} {time}";

        if (DateTime.TryParse(dateTimeStr, out var result))
        {
            return result;
        }

        return ParseDate(date);
    }

    #endregion
}
